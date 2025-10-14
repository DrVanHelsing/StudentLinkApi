import React from 'react';
import ICONS from '../utils/icons';

const SectionFeedback = ({ section, feedback, score }) => {
  const sectionNames = {
    contact: 'Contact Information',
    summary: 'Professional Summary',
    experience: 'Work Experience',
    education: 'Education',
    skills: 'Skills'
  };

  const sectionIcons = {
    contact: ICONS.email,
    summary: ICONS.lightbulb,
    experience: ICONS.briefcase,
    education: ICONS.education,
    skills: ICONS.skills
  };

  const getScoreColor = (score) => {
    if (score >= 0.8) return 'text-green-600 bg-green-50';
    if (score >= 0.6) return 'text-yellow-600 bg-yellow-50';
    return 'text-red-600 bg-red-50';
  };

  const getScoreLabel = (score) => {
    if (score >= 0.8) return 'Excellent';
    if (score >= 0.6) return 'Good';
    if (score >= 0.4) return 'Fair';
    return 'Needs Work';
  };

  const percentage = Math.round(score * 100);

  // Transform raw feedback text into a cleaner list/paragraph layout
  const renderFeedback = (text) => {
    if (!text) return <p className="text-gray-400 italic">No feedback available for this section.</p>;

    // Normalize spacing then insert newlines before numbered list items that are jammed together
    let normalized = text
      .replace(/\s+/g, ' ') // collapse excessive whitespace
      .replace(/(?<!^)\s+(?=\d+\.)/g, '\n') // add newline before 2.,3.,... when inline
      .replace(/\n\s*(\d+\.)/g, '\n$1');

    // Split into lines either by explicit newlines or bullet/number indicators
    let lines = normalized
      .split(/\n+/)
      .map(l => l.trim())
      .filter(Boolean);

    // If we still only have one long line, try splitting on numbered pattern within the line
    if (lines.length === 1 && /\d+\./.test(lines[0])) {
      lines = lines[0]
        .replace(/(?<=\.)\s+(?=\d+\.)/g, '\n')
        .split(/\n+/)
        .map(l => l.trim())
        .filter(Boolean);
    }

    // Detect if majority of lines start with a number or dash/bullet => treat as list
    const listLike = lines.length > 1 && lines.filter(l => /^((\d+\.)|[-•])\s+/.test(l)).length >= Math.max(2, Math.floor(lines.length * 0.6));

    if (listLike) {
      const items = lines.map(l => l.replace(/^((\d+)\.|[-•])\s*/, ''));
      return (
        <ul className="list-disc pl-5 space-y-1 text-gray-700">
          {items.map((it, i) => (
            <li key={i} className="leading-relaxed">{it}</li>
          ))}
        </ul>
      );
    }

    // Otherwise split into sentences for readability
    const sentences = normalized
      .split(/(?<=[.!?])\s+(?=[A-Z0-9])/)
      .map(s => s.trim())
      .filter(Boolean);

    if (sentences.length > 1) {
      return (
        <div className="space-y-2 text-gray-700">
          {sentences.map((s, i) => (
            <p key={i} className="leading-relaxed">{s}</p>
          ))}
        </div>
      );
    }

    return <p className="text-gray-700 leading-relaxed">{text}</p>;
  };

  return (
    <div className="bg-white rounded-xl shadow-md p-6 border-l-4 border-blue-500 hover:shadow-lg transition-shadow">
      <div className="flex items-start justify-between mb-4">
        <div className="flex items-center gap-3">
          <span className="text-3xl" aria-hidden>{sectionIcons[section]}</span>
          <div>
            <h3 className="text-lg font-bold text-gray-900 flex items-center gap-2">
              {sectionNames[section]}
              <span className={`text-xs font-semibold px-2 py-1 rounded-full ${getScoreColor(score)}`}>
                {getScoreLabel(score)}
              </span>
            </h3>
          </div>
        </div>
        <div className="text-right">
          <div className={`text-2xl font-bold ${getScoreColor(score).split(' ')[0]}`}>{percentage}%</div>
          <div className="text-xs text-gray-500">Score</div>
        </div>
      </div>

      {/* Progress Bar */}
      <div className="w-full bg-gray-200 rounded-full h-2 mb-4">
        <div
          className={`h-2 rounded-full transition-all duration-500 ${
            score >= 0.8 ? 'bg-green-500' : score >= 0.6 ? 'bg-yellow-500' : 'bg-red-500'
          }`}
          style={{ width: `${percentage}%` }}
        />
      </div>

      {/* Feedback Body */}
      {renderFeedback(feedback)}
    </div>
  );
};

export default SectionFeedback;