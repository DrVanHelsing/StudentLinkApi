import React, { useState, useEffect, useRef } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import SectionFeedback from '../components/SectionFeedback';
import ImprovementList from '../components/ImprovementList';
import ScoreGauge from '../components/ScoreGauge';
import { getInteractiveFeedback, markActionCompleted } from '../services/interactiveFeedbackApi';
import ICONS from '../utils/icons';

const POLL_INTERVAL_MS = 2000;
const POLL_TIMEOUT_MS = 30000; // 30s safety timeout

const InteractiveFeedbackPage = () => {
  const { cvId } = useParams();
  const navigate = useNavigate();
  const [feedback, setFeedback] = useState(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  const pollTimer = useRef(null);
  const pollStart = useRef(null);

  // Helper to format multi-line / numbered AI text (Next Steps, Progress Update)
  const renderRichText = (text, colorClass = 'text-blue-800') => {
    if (!text) return null;

    // Normalize spaces then create newlines before inline numbered items
    let normalized = text
      .replace(/\r/g, '')
      .replace(/\t/g, ' ')
      .replace(/ +/g, ' ')
      .replace(/ (?=\d+\.)/g, '\n') // space before number -> newline
      .trim();

    // Split on newlines
    let lines = normalized.split(/\n+/).map(l => l.trim()).filter(Boolean);

    // If still single line containing many numbered points, split them
    if (lines.length === 1 && /(\d+\.)/.test(lines[0])) {
      lines = lines[0]
        .replace(/(?<=\.) (?=\d+\.)/g, '\n')
        .split(/\n+/)
        .map(l => l.trim())
        .filter(Boolean);
    }

    const listLike = lines.length > 1 && lines.filter(l => /^((\d+\.)|[-•])\s+/.test(l)).length >= Math.max(2, Math.floor(lines.length * 0.6));

    if (listLike) {
      const items = lines.map(l => l.replace(/^((\d+)\.|[-•])\s*/, ''));
      return (
        <ul className={`list-disc pl-5 space-y-1 leading-relaxed ${colorClass}`}>
          {items.map((it,i)=>(<li key={i}>{it}</li>))}
        </ul>
      );
    }

    // Fallback: split into sentences for readability
    const sentences = normalized.split(/(?<=[.!?])\s+(?=[A-Z0-9])/).map(s=>s.trim()).filter(Boolean);
    if (sentences.length > 1) {
      return (
        <div className={`space-y-2 leading-relaxed ${colorClass}`}>
          {sentences.map((s,i)=>(<p key={i}>{s}</p>))}
        </div>
      );
    }
    return <p className={`leading-relaxed ${colorClass}`}>{normalized}</p>;
  };

  useEffect(() => {
    loadFeedback(true);
    return () => { if (pollTimer.current) clearTimeout(pollTimer.current); };
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [cvId]);

  const schedulePoll = () => {
    if (!pollStart.current) pollStart.current = Date.now();
    if (Date.now() - pollStart.current > POLL_TIMEOUT_MS) {
      setLoading(false);
      setError('Feedback is still processing. Please try again shortly.');
      return;
    }
    pollTimer.current = setTimeout(() => loadFeedback(false), POLL_INTERVAL_MS);
  };

  const loadFeedback = async (initial = false) => {
    try {
      if (initial) {
        setLoading(true); setError(null);
      }
      const data = await getInteractiveFeedback(cvId);
      if (data?.status === 'Processing') { setFeedback(data); setLoading(true); setError(null); schedulePoll(); return; }
      setFeedback(data); setError(null); setLoading(false);
    } catch (err) {
      const status = err.response?.status;
      if ([202,409,404].includes(status)) { setError(null); setLoading(true); schedulePoll(); return; }
      setError(err.response?.data?.error || 'Failed to load feedback'); setLoading(false);
    }
  };

  const handleActionComplete = async (cvId, actionIndex) => {
    try { await markActionCompleted(cvId, actionIndex); await loadFeedback(false); } catch(e){ console.error('Failed to mark action as completed:', e);} }

  if (loading && (!feedback || feedback?.status === 'Processing')) {
    return (
      <div className="max-w-5xl mx-auto py-12">
        <div className="bg-white rounded-xl shadow p-8 text-center">
          <div className="animate-spin inline-block w-12 h-12 border-4 border-blue-200 border-t-blue-600 rounded-full" />
          <h2 className="mt-4 text-xl font-semibold text-gray-900">Analyzing your CV...</h2>
          <p className="mt-2 text-gray-600">This usually takes a few seconds. We will update the page automatically.</p>
        </div>
      </div>
    );
  }

  if (error) {
    return (
      <div className="max-w-3xl mx-auto py-12">
        <div className="bg-red-50 border-l-4 border-red-400 p-6 rounded">
          <h2 className="text-red-800 font-semibold text-lg">Error Loading Feedback</h2>
          <p className="text-red-700 mt-1">{error}</p>
          <div className="mt-4">
            <button onClick={() => loadFeedback(true)} className="px-4 py-2 bg-red-600 text-white rounded hover:bg-red-700">Retry</button>
          </div>
        </div>
      </div>
    );
  }

  if (!feedback) return null;
  const sections = feedback.sections || {};

  return (
    <div className="max-w-6xl mx-auto">
      {/* Header */}
      <div className="bg-white rounded-xl shadow p-6 mb-6">
        <div className="flex items-center justify-between">
          <div>
            <h1 className="text-3xl font-bold text-gray-900">Interactive Feedback</h1>
            <p className="text-gray-600">Detailed analysis and action plan</p>
          </div>
          <div className="flex items-center gap-6">
            <div className="text-center">
              <div className="text-sm text-gray-500">Overall Score</div>
              <ScoreGauge score={feedback.overallScore || 0} />
            </div>
            <div>
              {feedback.isApproved ? (
                <span className="inline-block px-3 py-1 bg-green-100 text-green-800 rounded-full text-sm font-medium">{ICONS.approved} Approved</span>
              ) : (
                <span className="inline-block px-3 py-1 bg-yellow-100 text-yellow-800 rounded-full text-sm font-medium">{ICONS.pending} In Progress</span>
              )}
            </div>
          </div>
        </div>

        {/* Next Steps */}
        {feedback.nextSteps && (
          <div className="mt-6 bg-blue-50 border-l-4 border-blue-500 p-5 rounded-r">
            <div className="flex items-start gap-3">
              <span className="text-2xl" aria-hidden>{ICONS.goal}</span>
              <div className="flex-1">
                <h3 className="font-semibold text-blue-900 mb-2">Next Steps</h3>
                {renderRichText(feedback.nextSteps, 'text-blue-800')}
              </div>
            </div>
          </div>
        )}

        {/* Improvement from Previous */}
        {feedback.improvementFromPrevious && (
          <div className="mt-4 bg-green-50 border-l-4 border-green-500 p-5 rounded-r">
            <div className="flex items-start gap-3">
              <span className="text-2xl" aria-hidden>{ICONS.trending}</span>
              <div className="flex-1">
                <h3 className="font-semibold text-green-900 mb-2">Progress Update</h3>
                {renderRichText(feedback.improvementFromPrevious, 'text-green-800')}
              </div>
            </div>
          </div>
        )}
      </div>

      {/* Main Content Grid */}
      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
        <div className="lg:col-span-2 space-y-4">
          <h2 className="text-2xl font-bold text-gray-900 mb-4">Section Analysis</h2>
          <SectionFeedback section="contact" feedback={sections.contact?.feedback} score={sections.contact?.score || 0} />
          <SectionFeedback section="summary" feedback={sections.summary?.feedback} score={sections.summary?.score || 0} />
            <SectionFeedback section="experience" feedback={sections.experience?.feedback} score={sections.experience?.score || 0} />
          <SectionFeedback section="education" feedback={sections.education?.feedback} score={sections.education?.score || 0} />
          <SectionFeedback section="skills" feedback={sections.skills?.feedback} score={sections.skills?.score || 0} />
        </div>
        <div className="lg:col-span-1">
          <div className="sticky top-6">
            <ImprovementList priorities={feedback.improvementPriorities || []} onComplete={handleActionComplete} cvId={cvId} />
            <div className="mt-6 space-y-3">
              <button onClick={() => navigate('/cv-upload')} className="w-full px-4 py-3 bg-blue-600 text-white rounded-lg hover:bg-blue-700 transition-colors font-medium flex items-center justify-center gap-2">
                <span>{ICONS.upload}</span> Upload Improved Version
              </button>
              <button onClick={() => navigate('/cv-progress')} className="w-full px-4 py-3 bg-gray-200 text-gray-800 rounded-lg hover:bg-gray-300 transition-colors font-medium flex items-center justify-center gap-2">
                <span>{ICONS.chart}</span> View Progress Dashboard
              </button>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
};

export default InteractiveFeedbackPage;