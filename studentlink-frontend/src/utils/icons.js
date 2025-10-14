// Icon library using Unicode escape sequences (safe in non-UTF8 editors)
// This prevents emojis being replaced by '??' when the file is saved with a codepage
// Each value is a plain JS string built from Unicode code points.

export const ICONS = {
  // CV/Document icons
  document: '\u{1F4C4}', // ??
  cv: '\u{1F4C4}',
  upload: '\u{1F4E4}', // ??
  download: '\u{1F4E5}', // ??
  file: '\u{1F4C1}', // ??

  // User/People icons
  user: '\u{1F464}', // ??
  students: '\u{1F465}', // ??
  recruiter: '\u{1F4BC}', // ??
  admin: '\u2699\uFE0F', // ??
  profile: '\u{1F464}',

  // Action icons
  edit: '\u270F\uFE0F', // ??
  delete: '\u{1F5D1}\uFE0F', // ???
  save: '\u{1F4BE}', // ??
  cancel: '\u274C', // ?
  check: '\u2705', // ?
  close: '\u274C',
  search: '\u{1F50D}', // ??
  filter: '\u25BC', // ?
  apply: '\u2709\uFE0F', // ??

  // Job/Work icons
  job: '\u{1F4BC}', // ??
  briefcase: '\u{1F4BC}',
  location: '\u{1F4CD}', // ??
  calendar: '\u{1F4C5}', // ??
  time: '\u23F0', // ?
  money: '\u{1F4B0}', // ??
  salary: '\u{1F4B5}', // ??

  // Status icons
  approved: '\u2705', // ?
  pending: '\u23F3', // ?
  rejected: '\u274C', // ?
  processing: '\u2699\uFE0F', // ??
  warning: '\u26A0\uFE0F', // ??
  info: '\u2139\uFE0F', // ??
  active: '\u2705', // ?

  // Progress/Achievement icons
  trophy: '\u{1F3C6}', // ??
  star: '\u2B50', // ?
  medal: '\u{1F3C5}', // ??
  chart: '\u{1F4CA}', // ??
  trending: '\u{1F4C8}', // ??
  goal: '\u{1F3AF}', // ??

  // Communication icons
  email: '\u{1F4E7}', // ??
  phone: '\u{1F4F1}', // ??
  message: '\u{1F4AC}', // ??
  notification: '\u{1F514}', // ??

  // General icons
  home: '\u{1F3E0}', // ??
  dashboard: '\u{1F4CA}', // ??
  settings: '\u2699\uFE0F', // ??
  help: '\u2753', // ?
  lightbulb: '\u{1F4A1}', // ??
  rocket: '\u{1F680}', // ??
  celebrate: '\u{1F389}', // ??
  thumbsUp: '\u{1F44D}', // ??
  next: '\u25B6\uFE0F', // ??

  // Education icons
  education: '\u{1F393}', // ??
  school: '\u{1F3EB}', // ??
  book: '\u{1F4DA}', // ??
  certificate: '\u{1F4DC}', // ??

  // Skills icons
  skills: '\u{1F6E0}\uFE0F', // ???
  tech: '\u{1F4BB}', // ??
  code: '\u{1F4BB}', // ??
  tools: '\u{1F527}' // ??
};

export default ICONS;
