import React from 'react';

// Helper to format date
const formatDistanceToNow = (date) => {
  const seconds = Math.floor((new Date() - new Date(date)) / 1000);
  const minutes = Math.floor(seconds / 60);
  const hours = Math.floor(minutes / 60);
  const days = Math.floor(hours / 24);

  if (days > 0) return `${days} day${days > 1 ? 's' : ''} ago`;
  if (hours > 0) return `${hours} hour${hours > 1 ? 's' : ''} ago`;
  if (minutes > 0) return `${minutes} minute${minutes > 1 ? 's' : ''} ago`;
  return 'just now';
};

const CVHistory = ({ cvs, onViewFeedback, currentCvId }) => {
  if (!cvs || cvs.length === 0) {
    return (
      <div className="bg-gray-50 rounded-lg p-6 text-center">
        <p className="text-gray-600">No CV history available.</p>
      </div>
    );
  }

  const getScoreColor = (score) => {
    if (score >= 0.8) return 'text-green-600';
    if (score >= 0.6) return 'text-yellow-600';
    return 'text-red-600';
  };

  const getScoreBg = (score) => {
    if (score >= 0.8) return 'bg-green-50 border-green-200';
    if (score >= 0.6) return 'bg-yellow-50 border-yellow-200';
    return 'bg-red-50 border-red-200';
  };

  return (
    <div className="space-y-3">
      <h3 className="text-lg font-semibold text-gray-900 mb-4">CV History</h3>
      
      {cvs.map((cv, index) => (
        <div
          key={cv.id}
          className={`bg-white rounded-lg border-2 p-4 transition-all ${
            cv.id === currentCvId
              ? 'border-blue-500 shadow-md'
              : 'border-gray-200 hover:border-blue-300 hover:shadow-sm'
          }`}
        >
          <div className="flex items-center justify-between">
            <div className="flex-1">
              <div className="flex items-center space-x-2">
                <span className="text-lg">??</span>
                <div>
                  <p className="font-medium text-gray-900">{cv.fileName}</p>
                  <p className="text-xs text-gray-500">
                    Uploaded {formatDistanceToNow(cv.uploadedAt)}
                  </p>
                </div>
              </div>
              
              {cv.id === currentCvId && (
                <span className="inline-block mt-2 px-2 py-1 text-xs font-medium bg-blue-100 text-blue-800 rounded">
                  Current CV
                </span>
              )}
            </div>

            <div className="flex items-center space-x-4">
              {cv.feedback && (
                <div className={`px-4 py-2 rounded-lg border ${getScoreBg(cv.feedback.overallScore)}`}>
                  <div className="text-center">
                    <div className={`text-2xl font-bold ${getScoreColor(cv.feedback.overallScore)}`}>
                      {Math.round(cv.feedback.overallScore * 100)}%
                    </div>
                    <div className="text-xs text-gray-600 mt-1">
                      {cv.feedback.isApproved ? '? Approved' : '? In Progress'}
                    </div>
                  </div>
                </div>
              )}

              <button
                onClick={() => onViewFeedback(cv.id)}
                className="px-4 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700 transition-colors text-sm font-medium"
              >
                View Feedback
              </button>
            </div>
          </div>

          {/* Version indicator */}
          {index === 0 && cvs.length > 1 && (
            <div className="mt-3 pt-3 border-t border-gray-200">
              <p className="text-xs text-gray-600">
                ?? Version {cvs.length} of {cvs.length}
              </p>
            </div>
          )}
        </div>
      ))}
    </div>
  );
};

export default CVHistory