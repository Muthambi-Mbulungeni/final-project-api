import { Check, Lock, ShieldCheck } from 'lucide-react'
import logo from '../../assets/strategies-logo.png'
import { CASE_STAGES } from '../../config/caseStages'

const TRUST_POINTS = [
  { icon: Check, text: 'Live status for every stage of your case' },
  { icon: Lock, text: 'Documents sent securely to your consultant' },
  { icon: ShieldCheck, text: 'Personal information protected under POPIA' },
]

// Staggered entrance; only applied with motion-safe, so "reduce motion" shows it all at once.
const delay = (seconds) => ({ animationDelay: `${seconds}s` })

// The sign-in story, built from the real five-stage lifecycle rather than an illustration.
// Desktop: a dark panel with the stages as a vertical line. Phones: the same idea on white, the
// stages as a horizontal line under the headline, so the form still sits on the first screen.
// The headline is a paragraph, not a heading: the page's own h1 (e.g. "Welcome back") stays the first heading.
export default function JourneyIntro() {
  return (
    <aside
      aria-label="About the client portal"
      className="bg-white px-7 pt-9 lg:flex lg:flex-col lg:justify-center lg:bg-[#171412] lg:px-20 lg:py-16 motion-safe:lg:animate-fade-soft"
    >
      {/* The brand appears once per screen: this logo on phones, the form side's logo on desktop. */}
      <img src={logo} alt="Strategies Migration Services" className="h-[34px] w-auto motion-safe:animate-rise-soft lg:hidden" />
      <span className="hidden h-0.5 w-10 rounded-full bg-brand-600 lg:block motion-safe:lg:animate-rise-soft" aria-hidden="true" style={delay(0.2)} />

      <p
        className="mt-6 max-w-[14ch] text-[30px] leading-[1.08] font-semibold tracking-[-0.028em] text-balance text-stone-900 motion-safe:animate-rise-soft lg:mt-7 lg:text-[42px] lg:text-stone-50"
        style={delay(0.28)}
      >
        Your path, one clear step at a time.
      </p>
      <p
        className="mt-4 hidden max-w-[30ch] text-base leading-relaxed text-stone-400 lg:block motion-safe:lg:animate-rise-soft"
        style={delay(0.36)}
      >
        Track your application through every stage, see what your consultant needs, and send documents securely.
      </p>

      <ol
        aria-label="The five stages of an application"
        className="relative mt-7 flex max-w-[280px] justify-between lg:mt-12 lg:max-w-none lg:flex-col lg:justify-start"
      >
        <span
          aria-hidden="true"
          className="absolute top-1 right-1 left-1 h-px bg-stone-200 lg:top-[19px] lg:right-auto lg:bottom-[19px] lg:h-auto lg:w-px lg:bg-[#3a3532]"
        />
        <span
          aria-hidden="true"
          className="absolute top-1 right-1 left-1 h-px origin-left bg-brand-600 motion-safe:animate-draw-x lg:top-[19px] lg:right-auto lg:bottom-[19px] lg:h-auto lg:w-px lg:origin-top motion-safe:lg:animate-draw-y"
          style={delay(0.5)}
        />
        {CASE_STAGES.map((stage, index) => (
          <li key={stage.key} className="relative flex items-center gap-[18px] lg:py-2">
            <span
              aria-hidden="true"
              className="size-[9px] shrink-0 rounded-full bg-brand-600 shadow-[0_0_0_4px_rgba(196,22,28,0.14)] motion-safe:animate-dot-in lg:shadow-[0_0_0_4px_rgba(196,22,28,0.2)]"
              style={delay(0.55 + index * 0.25)}
            />
            <span className="sr-only text-[15px] text-stone-300 lg:not-sr-only">{stage.label}</span>
          </li>
        ))}
      </ol>
      <p className="mt-3.5 text-[13px] text-stone-500 motion-safe:animate-rise-soft lg:hidden" style={delay(1.6)}>
        Five clear stages, from enquiry to decision.
      </p>

      {/* stone-400 on the dark panel: stone-500 would fall below 4.5:1 there. */}
      <ul className="mt-14 hidden flex-wrap gap-x-5 gap-y-2 text-[13px] text-stone-400 lg:flex">
        {TRUST_POINTS.map(({ icon: Icon, text }, index) => (
          <li key={text} className="flex items-center gap-2 motion-safe:animate-rise-soft" style={delay(1.7 + index * 0.08)}>
            <Icon className="size-4" strokeWidth={1.75} aria-hidden="true" />
            {text}
          </li>
        ))}
      </ul>
    </aside>
  )
}
