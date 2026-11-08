// The sections a page's SectionNav (components/SectionNav.tsx) jumps to

export interface PageSection {
  /** The id of the section element (give it `{...sectionProps(id)}`) */
  id: string;
  label: string;
}

/** Spread onto each section a SectionNav links to: its id, a scroll offset clear of the sticky bars, and focusability for the jump */
export const sectionProps = (id: string) => ({
  id,
  'data-section': '',
  tabIndex: -1,
});
