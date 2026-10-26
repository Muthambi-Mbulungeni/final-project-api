#!/usr/bin/env fish
# Starts Postgres (Docker), the API and the web app, and prints the demo logins.
# Run from anywhere:  fish dev.fish      Stop everything with Ctrl+C.
# If something already uses a port (5151 API, 5173 web, 5432 database), it is stopped and replaced.
# Logs go to .devlogs/ (add .devlogs/ to .gitignore).

set root (dirname (realpath (status filename)))
cd $root
mkdir -p .devlogs

function banner
    set_color --bold cyan; echo $argv; set_color normal
end

function warn
    set_color yellow; echo "  $argv"; set_color normal
end

function fail
    set_color red; echo "ERROR: $argv"; set_color normal
    exit 1
end

# Process ids listening on a TCP port (works with lsof, fuser or ss, whichever exists).
function port_pids --argument-names port
    if command -q lsof
        lsof -ti tcp:$port -sTCP:LISTEN 2>/dev/null
    else if command -q fuser
        fuser $port/tcp 2>/dev/null | string match -r -a '\d+'
    else if command -q ss
        ss -ltnpH "sport = :$port" 2>/dev/null | string match -r -a -g 'pid=(\d+)'
    end
end

# Frees a port by killing whatever listens on it, escalating to kill -9 and then sudo.
function free_port --argument-names port
    set -l pids (port_pids $port | sort -u)
    test (count $pids) -eq 0; and return 0

    warn "port $port is busy (pid $pids): taking it over"
    kill $pids 2>/dev/null
    sleep 1
    set -l left (port_pids $port | sort -u)
    if test (count $left) -gt 0
        kill -9 $left 2>/dev/null
        sleep 1
    end
    set left (port_pids $port | sort -u)
    if test (count $left) -gt 0
        warn "needs admin rights, trying sudo"
        sudo kill -9 $left 2>/dev/null
        sleep 1
    end
    if test (count (port_pids $port)) -gt 0
        fail "could not free port $port. Run: sudo fuser -k $port/tcp"
    end
end

# Old runs of this script that were closed without Ctrl+C.
pkill -f 'Strategies.Api' 2>/dev/null
pkill -f 'strategies-web.*vite' 2>/dev/null

banner "1/4 Database (Docker)"
if not command -q docker
    fail "docker is not installed. sudo pacman -S docker docker-compose"
end
if not docker info >/dev/null 2>&1
    warn "Docker is not running, trying to start it"
    sudo systemctl start docker
    sleep 3
    docker info >/dev/null 2>&1; or fail "Docker would not start. Check: systemctl status docker"
end

# Port 5432 is only freed when it is not already our own container.
set running (docker ps --filter name=smp-postgres --filter status=running -q)
if test -z "$running"
    if test (count (port_pids 5432)) -gt 0
        # a system PostgreSQL service would just restart, so stop the service first
        if systemctl is-active --quiet postgresql 2>/dev/null
            warn "stopping the system postgresql service (it uses port 5432)"
            sudo systemctl stop postgresql
        end
        free_port 5432
    end
    docker rm -f smp-postgres >/dev/null 2>&1   # removes a stopped or half-created container
end

docker compose up -d smp-postgres >.devlogs/docker.log 2>&1
or begin
    tail -n 15 .devlogs/docker.log
    fail "could not start the database container (full log: .devlogs/docker.log)"
end

set ready 0
for i in (seq 40)
    if docker exec smp-postgres pg_isready -U postgres -d strategies_db >/dev/null 2>&1
        set ready 1
        break
    end
    sleep 1
end
test $ready -eq 1; or fail "Postgres did not become ready in 40 s. Check: docker logs smp-postgres"

banner "2/4 Migrations"
dotnet ef database update --project Strategies.Api >.devlogs/migrate.log 2>&1
or begin
    tail -n 15 .devlogs/migrate.log
    fail "migrations failed (full log: .devlogs/migrate.log). Is dotnet-ef installed? dotnet tool install --global dotnet-ef"
end

# load seed data when the database is empty
set users (docker exec smp-postgres psql -U postgres -d strategies_db -tAc 'SELECT count(*) FROM "User"' 2>/dev/null | string trim)
if test "$users" = "0"
    banner "Database is empty: loading demo and mock data"
    for f in 1-reference-and-users 2-cases-and-activity 3-mock-data
        test -f database/seed/$f.sql; or begin
            warn "database/seed/$f.sql not found, skipping"
            continue
        end
        docker exec -i smp-postgres psql -q -U postgres -d strategies_db -v ON_ERROR_STOP=1 -f /seed/$f.sql >.devlogs/seed-$f.log 2>&1
        or warn "$f.sql failed (see .devlogs/seed-$f.log)"
    end
end

banner "3/4 Starting API and web app"
free_port 5151
free_port 5173

dotnet run --project Strategies.Api --launch-profile http >.devlogs/api.log 2>&1 &
set api_pid $last_pid

if not test -d strategies-web/node_modules
    echo "  installing web dependencies (first run)..."
    npm --prefix strategies-web ci >.devlogs/npm.log 2>&1
    or fail "npm ci failed (see .devlogs/npm.log). Node 22 needed: node -v"
end
# --strictPort: fail loudly instead of silently moving to another port
npm --prefix strategies-web run dev -- --port 5173 --strictPort >.devlogs/web.log 2>&1 &
set web_pid $last_pid

function cleanup --on-signal INT --on-signal TERM
    echo
    echo "Stopping API and web app..."
    for pid in $api_pid $web_pid
        pkill -P $pid 2>/dev/null
        kill $pid 2>/dev/null
    end
    sleep 1
    free_port 5151 >/dev/null 2>&1
    free_port 5173 >/dev/null 2>&1
    echo "Done. The database container keeps running (docker compose stop to stop it)."
    exit 0
end

# wait for both servers (max 90 s), stopping early if one of them crashed
set api_up 0
set web_up 0
for i in (seq 90)
    test $api_up -eq 0; and curl -fs http://localhost:5151/health >/dev/null 2>&1; and set api_up 1
    test $web_up -eq 0; and curl -fs http://localhost:5173 >/dev/null 2>&1; and set web_up 1
    test $api_up -eq 1 -a $web_up -eq 1; and break
    test $api_up -eq 0; and not kill -0 $api_pid 2>/dev/null; and break
    test $web_up -eq 0; and not kill -0 $web_pid 2>/dev/null; and break
    sleep 1
end

banner "4/4 Status"
if test $api_up -eq 1
    echo "API  : http://localhost:5151   (Swagger: /swagger)"
else
    set_color red; echo "API  : NOT RUNNING. Last log lines:"; set_color normal
    tail -n 15 .devlogs/api.log
end
if test $web_up -eq 1
    echo "Web  : http://localhost:5173"
else
    set_color red; echo "Web  : NOT RUNNING. Last log lines:"; set_color normal
    tail -n 15 .devlogs/web.log
end
echo
banner "Test logins"
printf "%-12s %-38s %s\n" ROLE EMAIL PASSWORD
printf "%-12s %-38s %s\n" Admin admin@nexus.com 'AdminPassword123!'
printf "%-12s %-38s %s\n" Consultant consultant@nexus.com 'ConsultantPassword123!'
printf "%-12s %-38s %s\n" Consultant riaad@nexus.com 'ConsultantPassword123!'
printf "%-12s %-38s %s\n" Consultant 'mock.consultant1..3@example.com' 'ConsultantPassword123!'
printf "%-12s %-38s %s\n" Client client@nexus.com 'ClientPassword123!'
printf "%-12s %-38s %s\n" Client 'mock.client01..15@example.com' 'ClientPassword123!'
printf "%-12s %-38s %s\n" "(suspended)" mock.suspended@example.com 'ClientPassword123!'
printf "%-12s %-38s %s\n" "(deleted)" mock.deleted@example.com 'ClientPassword123!'
printf "%-12s %-38s %s\n" "(locked)" mock.locked@example.com 'ClientPassword123!  (locked ~15 min after seeding)'
echo
echo "5 wrong passwords lock an account for 15 min. To unlock all:"
echo "  docker exec smp-postgres psql -U postgres -d strategies_db -c 'UPDATE \"User\" SET \"FailedLoginAttempts\"=0, \"LockoutEnd\"=NULL;'"
echo
if test $api_up -eq 1 -a $web_up -eq 1
    echo "Live logs below. Press Ctrl+C to stop API and web."
    echo "------------------------------------------------------------"
    tail -n 0 -f .devlogs/api.log .devlogs/web.log
else
    fail "something did not start (see the log lines above). Fix it and run the script again."
end
