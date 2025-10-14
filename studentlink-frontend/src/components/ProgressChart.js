import React from 'react';

const ProgressChart = ({ initialScore, currentScore, improvementPercentage }) => {
  const getImprovementColor = (percentage) => {
    if (percentage > 50) return 'text-green-600';
    if (percentage > 20) return 'text-yellow-600';
    if (percentage > 0) return 'text-blue-600';
    return 'text-gray-600';
  };

  const getArrowIcon = (percentage) => {
    if (percentage > 0) return '??';
    if (percentage < 0) return '??';
    return '??';
  };

  return (
    <div className="bg-gradient-to-br from-blue-50 to-indigo-50 rounded-lg p-6 border border-blue-200">
      <h3 className="text-lg font-semibold text-gray-900 mb-4">Your Progress</h3>

      {/* Score Comparison */}
      <div className="flex items-center justify-between mb-6">
        <div className="text-center">
          <div className="text-sm text-gray-600 mb-1">Initial Score</div>
          <div className="text-3xl font-bold text-gray-700">
            {Math.round(initialScore * 100)}%
          </div>
        </div>

        <div className="flex flex-col items-center">
          <span className="text-3xl mb-2">{getArrowIcon(improvementPercentage)}</span>
          <div className={`text-lg font-semibold ${getImprovementColor(improvementPercentage)}`}>
            {improvementPercentage > 0 && '+'}
            {improvementPercentage.toFixed(1)}%
          </div>
        </div>

        <div className="text-center">
          <div className="text-sm text-gray-600 mb-1">Current Score</div>
          <div className="text-3xl font-bold text-blue-600">
            {Math.round(currentScore * 100)}%
          </div>
        </div>
      </div>

      {/* Visual Progress Bar */}
      <div className="relative h-8 bg-gray-200 rounded-full overflow-hidden">
        {/* Initial score marker */}
        <div
          className="absolute top-0 bottom-0 w-1 bg-gray-400 z-10"
          style={{ left: `${initialScore * 100}%` }}
        >
          <div className="absolute -top-6 left-1/2 transform -translate-x-1/2 text-xs text-gray-600 whitespace-nowrap">
            Start
          </div>
        </div>

        {/* Current progress */}
        <div
          className="absolute top-0 left-0 h-full bg-gradient-to-r from-blue-400 to-blue-600 transition-all duration-1000 flex items-center justify-end pr-2"
          style={{ width: `${currentScore * 100}%` }}
        >
          <span className="text-xs font-semibold text-white">
            {Math.round(currentScore * 100)}%
          </span>
        </div>
      </div>

      {/* Improvement Message */}
      <div className="mt-4 text-center">
        {improvementPercentage > 50 && (
          <p className="text-sm text-green-700 font-medium">
            ?? Amazing progress! You've improved significantly!
          </p>
        )}
        {improvementPercentage > 20 && improvementPercentage <= 50 && (
          <p className="text-sm text-yellow-700 font-medium">
            ? Great job! Keep improving!
          </p>
        )}
        {improvementPercentage > 0 && improvementPercentage <= 20 && (
          <p className="text-sm text-blue-700 font-medium">
            ?? Good start! Continue refining your CV!
          </p>
        )}
        {improvementPercentage === 0 && (
          <p className="text-sm text-gray-700 font-medium">
            ?? This is your first upload. Upload an improved version to track progress!
          </p>
        )}
      </div>
    </div>
  );
};

export default ProgressChart;