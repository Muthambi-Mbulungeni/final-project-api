import { Link } from 'react-router-dom'
import AuthLayout from '../components/layout/AuthLayout'
import { useDocumentTitle } from '../lib/useDocumentTitle'

const LAST_UPDATED = '30 September 2026'

function Section({ title, children }) {
  return (
    <section className="mt-8">
      <h2 className="text-base font-semibold text-stone-900">{title}</h2>
      <div className="mt-2 space-y-3 text-sm leading-relaxed text-stone-700">{children}</div>
    </section>
  )
}

function List({ items }) {
  return (
    <ul className="list-disc space-y-1.5 pl-5">
      {items.map((item) => (
        <li key={item}>{item}</li>
      ))}
    </ul>
  )
}

// Details only the consultancy can supply are marked for their confirmation, never invented.
function ToConfirm({ children }) {
  return (
    <span className="inline-block rounded bg-amber-50 px-1.5 py-0.5 text-amber-800 ring-1 ring-amber-200 ring-inset">
      {children}
    </span>
  )
}

export default function PrivacyPage() {
  useDocumentTitle('Privacy notice')

  return (
    <AuthLayout wide>
      <p className="text-xs font-semibold uppercase tracking-[0.08em] text-brand-700">Privacy notice</p>
      <h1 className="mt-1 text-xl font-semibold tracking-tight text-stone-900">How we use your information</h1>
      <p className="mt-2 text-sm text-stone-600">
        How Strategies Migration Services handles personal information in this portal, under the Protection of
        Personal Information Act 4 of 2013 (POPIA). Last updated {LAST_UPDATED}.
      </p>

      <Section title="What we collect">
        <p>When you start an enquiry:</p>
        <List
          items={[
            'Your full name and email address',
            'Your current immigration status in South Africa',
            'When you would like to start',
            'Whether you need help preparing your application',
          ]}
        />
        <p>
          If you become a client, we also hold the details of your case and the documents needed for your
          application, such as passport or identity documents. To sign in, we keep your email address and a
          password, which is stored only as a secure one-way hash and never in readable form.
        </p>
      </Section>

      <Section title="Why we use it">
        <List
          items={[
            'To assess your enquiry and contact you about it',
            'To prepare and manage your immigration application',
            'To keep you informed about the progress of your case',
          ]}
        />
        <p>The portal does not share your information with advertisers or marketing services.</p>
      </Section>

      <Section title="Automated screening of enquiries">
        <p>
          An AI intake agent helps us prioritise new enquiries. It receives your name, your immigration status
          and whether you need help with your application, and is provided by Google Gemini on our behalf.
        </p>
        <p>
          The agent does not make final decisions. Enquiries it cannot qualify, or that arrive while it is
          unavailable, are reviewed by a consultant. You can ask for a person to review any outcome.
        </p>
        <p>
          Where the AI provider processes this information: <ToConfirm>To be confirmed by Strategies Migration Services</ToConfirm>
        </p>
      </Section>

      <Section title="Who can see it">
        <p>
          Your consultant and the practice administrators. As a client, you can see only your own case. The
          server checks every request against the signed-in person and their role, so hiding a screen is never
          the only protection.
        </p>
      </Section>

      <Section title="How we protect it">
        <List
          items={[
            'Connections to the portal are encrypted (HTTPS)',
            'Passwords are stored only as secure hashes',
            'Sign-in sessions expire, and staff screens sign out after 30 minutes without activity',
            'Changes to key records are recorded in an audit log',
          ]}
        />
      </Section>

      <Section title="How long we keep it">
        <p>
          Retention period: <ToConfirm>To be confirmed by Strategies Migration Services</ToConfirm>
        </p>
      </Section>

      <Section title="Your rights">
        <p>
          You may ask to see the personal information we hold about you, ask us to correct or delete it, and
          object to how it is used. If you are not satisfied with our response, you may complain to the
          Information Regulator (South Africa).
        </p>
        <p>
          Information officer and contact details: <ToConfirm>To be confirmed by Strategies Migration Services</ToConfirm>
        </p>
      </Section>

      <div className="mt-10 flex flex-col items-center gap-3 border-t border-stone-200 pt-6 text-sm sm:flex-row sm:justify-center sm:gap-6">
        <Link to="/enquire" className="font-medium text-brand-700 hover:text-brand-800">
          Start an enquiry
        </Link>
        <Link to="/login" className="font-medium text-stone-600 hover:text-stone-900">
          Go to sign in
        </Link>
      </div>
    </AuthLayout>
  )
}
