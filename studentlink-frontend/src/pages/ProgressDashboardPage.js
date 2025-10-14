import React, { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { getImprovementProgress, getInteractiveDashboard } from '../services/interactiveFeedbackApi';
import ICONS from '../utils/icons';

const ProgressDashboardPage = () => {
  const [dashboard, setDashboard] = useState(null);
  const [progress, setProgress] = useState(null);
  const [loading, setLoading] = useState(true);
  const navigate = useNavigate();

  useEffect(() => {
    loadData();
  }, []);

  const loadData = async () => {
    try {
      setLoading(true);
      const [dashData, progData] = await Promise.all([
        getInteractiveDashboard().catch(() => null),
        getImprovementProgress().catch(() => null)
      ]);
      setDashboard(dashData);
      setProgress(progData);
    } catch (error) {
      console.error('Error loading progress data:', error);
    } finally {
      setLoading(false);
    }
  };

  const handleViewFeedback = (cvId) => {
    navigate(`/cv-feedback/${cvId}`);
  };

  if (loading) {
    return (
      <div className="flex items-center justify-center min-h-screen">
        <div className="text-center">
          <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-blue-600 mx-auto"></div>
          <p className="mt-4 text-gray-600">Loading your progress...</p>
        </div>
      </div>
    );
  }

  const currentScore = progress?.currentScore || 0;
  const initialScore = progress?.initialScore || 0;
  const improvementPercentage = progress?.improvementPercentage || 0;
  const completedActions = progress?.completedActions || 0;
  const totalActions = progress?.totalActions || 0;
  const actionProgressPercentage = totalActions > 0 ? (completedActions / totalActions) * 100 : 0;

  return (
    <div className="max-w-7xl mx-auto">
      {/* Header */}
      <div className="mb-8">
        <h1 className="text-4xl font-bold text-gray-900 mb-2 flex items-center gap-3">
          <span className="text-5xl">{ICONS.chart}</span>
          CV Progress Dashboard
        </h1>
        <p className="text-gray-600 text-lg">Track your CV improvement journey</p>
      </div>

      {/* Stats Grid */}
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6 mb-8">
        <StatCard
          title="Current Score"
          value={`${Math.round(currentScore * 100)}%`}
          icon={ICONS.trophy}
          color="blue"
          description="Your latest CV quality score"
        />
        <StatCard
          title="Initial Score"
          value={`${Math.round(initialScore * 100)}%`}
          icon={ICONS.star}
          color="purple"
          description="Your first upload score"
        />
        <StatCard
          title="Total Uploads"
          value={progress?.totalUploads || 0}
          icon={ICONS.upload}
          color="green"
          description="Versions submitted"
        />
        <StatCard
          title="Improvement"
          value={improvementPercentage > 0 ? `+${Math.round(improvementPercentage)}%` : '0%'}
          icon={ICONS.trending}
          color={improvementPercentage > 0 ? 'green' : 'gray'}
          description="Overall improvement"
        />
      </div>

      {/* Main Content */}
      <div className="grid grid-cols-1 lg:grid-cols-3 gap-8">
        {/* Left Column - Progress Details */}
        <div className="lg:col-span-2 space-y-6">
          {/* Progress Chart */}
          <div className="bg-white rounded-xl shadow-md p-6">
            <h2 className="text-2xl font-bold text-gray-900 mb-6 flex items-center gap-2">
              <span>{ICONS.trending}</span>
              Score Progress
            </h2>
            
            <div className="mb-6">
              <div className="flex items-end justify-between mb-4">
                <div className="text-center flex-1">
                  <div className="text-sm text-gray-600 mb-2">Initial Score</div>
                  <div className="text-3xl font-bold text-purple-600">{Math.round(initialScore * 100)}%</div>
                </div>
                <div className="flex-shrink-0 px-4">
                  <div className="text-4xl">{ICONS.next}</div>
                </div>
                <div className="text-center flex-1">
                  <div className="text-sm text-gray-600 mb-2">Current Score</div>
                  <div className="text-3xl font-bold text-blue-600">{Math.round(currentScore * 100)}%</div>
                </div>
              </div>

              {/* Visual Progress Bar */}
              <div className="relative pt-1">
                <div className="flex mb-2 items-center justify-between">
                  <div>
                    <span className="text-xs font-semibold inline-block text-blue-600">
                      Progress
                    </span>
                  </div>
                  <div className="text-right">
                    <span className="text-xs font-semibold inline-block text-blue-600">
                      {Math.round(currentScore * 100)}%
                    </span>
                  </div>
                </div>
                <div className="overflow-hidden h-4 mb-4 text-xs flex rounded-full bg-gray-200">
                  <div
                    style={{ width: `${currentScore * 100}%` }}
                    className="shadow-none flex flex-col text-center whitespace-nowrap text-white justify-center bg-gradient-to-r from-purple-500 to-blue-600 transition-all duration-500"
                  ></div>
                </div>
              </div>

              {progress?.totalUploads === 1 ? (
                <div className="bg-blue-50 border border-blue-200 rounded-lg p-4 flex items-start gap-3">
                  <span className="text-2xl">{ICONS.info}</span>
                  <p className="text-blue-800">
                    This is your first upload. Upload an improved version to track progress!
                  </p>
                </div>
              ) : improvementPercentage > 0 ? (
                <div className="bg-green-50 border border-green-200 rounded-lg p-4 flex items-start gap-3">
                  <span className="text-2xl">{ICONS.celebrate}</span>
                  <p className="text-green-800">
                    Excellent work! You've improved your CV score by {Math.round(improvementPercentage)}%
                  </p>
                </div>
              ) : null}
            </div>
          </div>

          {/* Current CV Section */}
          {dashboard?.currentCV && (
            <div className="bg-white rounded-xl shadow-md p-6">
              <h2 className="text-2xl font-bold text-gray-900 mb-4 flex items-center gap-2">
                <span>{ICONS.document}</span>
                Current CV
              </h2>
              
              <div className="flex items-center justify-between p-4 bg-gray-50 rounded-lg border-2 border-gray-200 hover:border-blue-300 transition-colors">
                <div className="flex items-center gap-4">
                  <div className="text-5xl">{ICONS.cv}</div>
                  <div>
                    <div className="font-semibold text-gray-900 text-lg">
                      {dashboard.currentCV.fileName}
                    </div>
                    <div className="text-sm text-gray-600">
                      Uploaded {new Date(dashboard.currentCV.uploadedAt).toLocaleDateString()}
                    </div>
                  </div>
                </div>
                <div className="text-right">
                  <div className="inline-flex items-center justify-center w-20 h-20 rounded-full border-4 border-blue-500">
                    <span className="text-2xl font-bold text-blue-600">
                      {Math.round((dashboard.currentCV.qualityScore || 0) * 100)}%
                    </span>
                  </div>
                  <button
                    onClick={() => handleViewFeedback(dashboard.currentCV.id)}
                    className="mt-3 px-4 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700 transition-colors text-sm font-medium"
                  >
                    View Feedback
                  </button>
                </div>
              </div>
            </div>
          )}

          {/* Improvement Actions Details */}
          {dashboard?.nextSteps && (
            <div className="bg-white rounded-xl shadow-md p-6">
              <h2 className="text-2xl font-bold text-gray-900 mb-4 flex items-center gap-2">
                <span>{ICONS.lightbulb}</span>
                Recommended Next Steps
              </h2>
              
              <div className="bg-blue-50 border-l-4 border-blue-500 p-6 rounded-r-lg">
                <div className="prose prose-blue max-w-none">
                  <p className="text-blue-900 whitespace-pre-line leading-relaxed">
                    {dashboard.nextSteps}
                  </p>
                </div>
              </div>
            </div>
          )}

          {/* CV History */}
          {dashboard?.cvHistory && dashboard.cvHistory.length > 1 && (
            <div className="bg-white rounded-xl shadow-md p-6">
              <h2 className="text-2xl font-bold text-gray-900 mb-4 flex items-center gap-2">
                <span>{ICONS.calendar}</span>
                CV History
              </h2>
              
              <div className="space-y-3">
                {dashboard.cvHistory.map((cv, index) => (
                  <div
                    key={cv.id}
                    className="flex items-center justify-between p-4 bg-gray-50 rounded-lg border border-gray-200 hover:bg-gray-100 transition-colors"
                  >
                    <div className="flex items-center gap-3">
                      <div className="text-3xl">{ICONS.document}</div>
                      <div>
                        <div className="font-medium text-gray-900">{cv.fileName}</div>
                        <div className="text-sm text-gray-600">
                          {new Date(cv.uploadedAt).toLocaleDateString()} • {Math.round((cv.fileSize || 0) / 1024)} KB
                        </div>
                      </div>
                    </div>
                    <div className="flex items-center gap-4">
                      <div className="text-right">
                        <div className="text-sm text-gray-600">Score</div>
                        <div className="text-2xl font-bold text-blue-600">
                          {Math.round((cv.qualityScore || 0) * 100)}%
                        </div>
                      </div>
                      {index === 0 && (
                        <span className="px-3 py-1 bg-blue-100 text-blue-800 rounded-full text-xs font-semibold">
                          Latest
                        </span>
                      )}
                    </div>
                  </div>
                ))}
              </div>
            </div>
          )}
        </div>

        {/* Right Column - Action Progress */}
        <div className="lg:col-span-1">
          <div className="bg-white rounded-xl shadow-md p-6 sticky top-6">
            <h3 className="text-xl font-bold text-gray-900 mb-4 flex items-center gap-2">
              <span>{ICONS.goal}</span>
              Action Progress
            </h3>
            
            {progress && (
              <>
                <div className="flex items-center justify-between mb-4">
                  <span className="text-gray-600">Completed Actions</span>
                  <span className="text-3xl font-bold text-blue-600">
                    {completedActions}/{totalActions}
                  </span>
                </div>

                {/* Progress Bar */}
                <div className="relative pt-1 mb-6">
                  <div className="overflow-hidden h-6 mb-2 text-xs flex rounded-full bg-gray-200">
                    <div
                      style={{ width: `${actionProgressPercentage}%` }}
                      className="shadow-none flex flex-col text-center whitespace-nowrap text-white justify-center bg-gradient-to-r from-blue-500 to-green-500 transition-all duration-500"
                    >
                      {actionProgressPercentage > 15 && (
                        <span className="text-xs font-semibold">
                          {Math.round(actionProgressPercentage)}%
                        </span>
                      )}
                    </div>
                  </div>
                </div>

                {/* Milestone Messages */}
                <div className="space-y-3 mb-6">
                  {actionProgressPercentage === 100 && (
                    <div className="bg-green-50 border-2 border-green-200 rounded-lg p-4 text-center">
                      <span className="text-3xl block mb-2">{ICONS.celebrate}</span>
                      <p className="text-sm text-green-800 font-semibold">
                        All actions completed! Ready to upload!
                      </p>
                    </div>
                  )}
                  {actionProgressPercentage >= 75 && actionProgressPercentage < 100 && (
                    <div className="bg-blue-50 border-2 border-blue-200 rounded-lg p-4 text-center">
                      <span className="text-3xl block mb-2">{ICONS.rocket}</span>
                      <p className="text-sm text-blue-800 font-semibold">
                        Almost there! Just a few more!
                      </p>
                    </div>
                  )}
                  {actionProgressPercentage >= 50 && actionProgressPercentage < 75 && (
                    <div className="bg-yellow-50 border-2 border-yellow-200 rounded-lg p-4 text-center">
                      <span className="text-3xl block mb-2">{ICONS.thumbsUp}</span>
                      <p className="text-sm text-yellow-800 font-semibold">
                        Halfway there! Great progress!
                      </p>
                    </div>
                  )}
                  {actionProgressPercentage < 50 && actionProgressPercentage > 0 && (
                    <div className="bg-purple-50 border-2 border-purple-200 rounded-lg p-4 text-center">
                      <span className="text-3xl block mb-2">{ICONS.goal}</span>
                      <p className="text-sm text-purple-800 font-semibold">
                        Good start! Keep going!
                      </p>
                    </div>
                  )}
                </div>
              </>
            )}

            {/* Quick Actions */}
            <div className="space-y-3">
              <button
                onClick={() => navigate('/cv-upload')}
                className="w-full px-4 py-3 bg-blue-600 text-white rounded-lg hover:bg-blue-700 transition-colors font-medium flex items-center justify-center gap-2"
              >
                <span>{ICONS.upload}</span>
                Upload New Version
              </button>
              
              {dashboard?.currentCV && (
                <button
                  onClick={() => handleViewFeedback(dashboard.currentCV.id)}
                  className="w-full px-4 py-3 bg-gray-200 text-gray-800 rounded-lg hover:bg-gray-300 transition-colors font-medium flex items-center justify-center gap-2"
                >
                  <span>{ICONS.document}</span>
                  View Detailed Feedback
                </button>
              )}
            </div>

            {/* Motivational Quote */}
            <div className="mt-6 p-4 bg-gradient-to-br from-purple-50 to-blue-50 rounded-lg border border-purple-200">
              <p className="text-sm text-purple-900 italic text-center">
                "Every improvement brings you closer to your dream job!"
              </p>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
};

const StatCard = ({ title, value, icon, color, description }) => (
  <div className={`bg-white rounded-xl shadow-md p-6 border-l-4 border-${color}-500 hover:shadow-lg transition-shadow`}>
    <div className="flex items-center justify-between mb-2">
      <span className="text-3xl">{icon}</span>
      <span className={`text-3xl font-bold text-${color}-600`}>{value}</span>
    </div>
    <div className="text-sm font-semibold text-gray-700 mb-1">{title}</div>
    <div className="text-xs text-gray-500">{description}</div>
  </div>
);

export default ProgressDashboardPage;