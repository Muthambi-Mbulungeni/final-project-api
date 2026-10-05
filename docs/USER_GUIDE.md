# User guide

Strategies Migration Platform, web portal and Android app.

This guide gives step-by-step instructions for each kind of user. Words in **bold** are the exact labels on screen. For setting the system up, see the [README](../README.md).

- [1. Before you start (everyone)](#1-before-you-start-everyone)
- [2. Visitors: send an enquiry](#2-visitors-send-an-enquiry)
- [3. Clients](#3-clients)
- [4. Consultants](#4-consultants)
- [5. Administrators](#5-administrators)
- [6. When something goes wrong](#6-when-something-goes-wrong)

## 1. Before you start (everyone)

### Signing in

1. Open the web portal. The sign-in page opens.
2. Enter your **Email address** and password, then select **Sign in**.
3. You arrive at the home page for your role: **My case** for clients, **Overview** for consultants and administrators.

Accounts are created by the consultancy. For a demonstration, use the demo accounts in [database/seed/README.md](../database/seed/README.md).

### Finding your way around

- The menu lists the pages for your role. On a phone, open it with the menu button.
- The bell at the top shows your notifications and how many are unread. Open one and choose **Mark as read**.
- Panels that open over a page (a lead, a case) close with **Close** or the Escape key.
- Every page works with a keyboard alone and at phone width.

### Signing out

Choose **Sign out** in the menu. Staff accounts are signed out automatically after 30 minutes without activity, with a warning one minute before, so an unattended screen does not stay open.

## 2. Visitors: send an enquiry

Anyone can ask the consultancy for help without an account.

1. On the sign-in page, choose **Start an enquiry**.
2. **Step 1:** enter your **Full name** and **Email address**, choose **Your current status in South Africa**, then choose **Continue**.
3. **Step 2:** answer **When would you like to start?**, tick **I need help preparing my application** if you want a consultant to guide you, and tick the consent box. The consultancy can only process your details with your consent, as POPIA requires. The **Privacy notice** link explains how your details are used.
4. Choose **Submit enquiry**. You see a confirmation with your reference.

If a field is missing or invalid, the form says what to fix and moves you to that field. **Back** returns to step 1 without losing what you typed.

What happens next: the enquiry is checked automatically and passed to a consultant. If the automatic check cannot decide, a consultant reviews it personally, so every enquiry is read.

## 3. Clients

### Follow your case

1. Sign in. **My case** shows the five stages of your application: **Initial intake**, **Document gathering**, **Under review**, **Submitted to authority** and **Final decision**.
2. Finished stages are marked as done, and your current stage is highlighted.
3. **Next:** tells you what happens next and whether anything is needed from you.

### Replace a rejected document

When a consultant rejects a document, it shows under **Action needed** with the consultant's note explaining why.

1. On **My case**, choose **Replace** followed by the document's name, or open **Documents** from the menu.
2. Read the consultant's note on the rejected document.
3. Choose your file. It must be a PDF, JPG or PNG of 5 MB or less.
4. Choose **Send new copy**.
5. The document now shows as waiting for review. Your consultant reviews it, and you will see the result on this page.

### On your phone

The Android app shows the same case, stages, documents and notifications. Sign in with the same email address and password.

## 4. Consultants

### Overview

**Overview** shows your workload at a glance, with **View all** links to your leads and cases.

### Review new enquiries

1. Open **Lead triage**. Use the search box (name or email) and the **Verdict** filter to find leads; leads marked **Needs review** are the ones the automatic check could not decide.
2. Select a lead's name to open its details: immigration status, whether the person needs help, their preferred start date and the agent's reason for its verdict.
3. Under **Your decision**, choose **Mark qualified** or **Mark unqualified**.
4. A message confirms the decision, and the lead now shows you as the reviewer. You can change it later with **Change to qualified** or **Change to unqualified**.

### Manage a case

1. Open **My cases**. You see only the cases assigned to you.
2. Select the client's name to open the case.
3. To move the case on, choose the new stage and select **Move to** followed by the stage name. To correct a mistake, choose an earlier stage and select **Move back to** followed by the stage name.
4. The client sees the new stage straight away, and the change is recorded in the audit log.

### Review documents

1. When an open case has documents waiting, it shows **Needs your decision** at the top. Choose **Review** to jump to the documents.
2. Choose **Open file** to view the document in a new tab.
3. Choose **Approve**, or choose **Reject**, write a note that tells the client what to fix, and choose **Confirm rejection**. A rejection cannot be sent without a note.
4. The client sees your decision and note on their **Documents** page and can send a new copy.

### Export

On **My cases**, **Export CSV** downloads your cases as a spreadsheet file.

## 5. Administrators

Administrators can do everything consultants can, across all consultants' work.

### Overview

**Overview** shows totals for leads and cases, each consultant's workload against a target of 10 active cases, cases waiting for a consultant, and recent activity. Shortcuts: **Review enquiries**, **Assign in Cases** and **View audit log**.

### Assign a consultant to a case

1. Open **Cases** and select the client's name.
2. Under the consultant section, choose an active consultant and select **Assign** (or **Reassign** if the case already has one).
3. Use the workload figures on **Overview** to keep the cases evenly spread.

### Manage users

1. Open **Users** and search by name or email.
2. To stop someone signing in, choose **Suspend user** and confirm. Their records stay in place.
3. To let them back in, choose **Reinstate user** and confirm.
4. **Activity** opens the audit log filtered to that person.

### Read the audit log

**Audit log** lists who changed what, and when: stage changes, assignments, document and lead decisions, and account changes. Filter by user, and clear the filter to see everyone's actions again.

### Leads and export

**Leads** shows every enquiry with summary counts by verdict. **Export CSV** on **Cases** downloads all cases.

## 6. When something goes wrong

| What you see | What to do |
|---|---|
| "Too many sign-in attempts. Wait a minute and try again." | Sign-in is limited to 5 attempts a minute. Wait one minute, then sign in again. |
| Signed out unexpectedly | Staff are signed out after 30 minutes without activity. Sign in again. |
| Forgotten password | **Forgot password?** on the sign-in page is not finished in this release ([ISS-01](ISSUES.md#iss-01)). Contact the consultancy to have your password reset. |
| "Invalid file type" when uploading | Only PDF, JPG and PNG files are accepted. Save or scan the document in one of these formats. |
| "File size exceeds the 5MB limit" | Reduce the file size, for example by scanning at a lower resolution, and try again. |
| A page says it could not load | Check your connection and choose **Try again**. If it keeps happening, sign out and in again. |
| A page that is not found | Use **Return to the portal** to go back to your home page. |

Known problems and their status are listed in [ISSUES.md](ISSUES.md).
