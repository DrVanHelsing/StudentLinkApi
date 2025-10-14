import React from 'react';
import { useNavigate } from 'react-router-dom';

const CVUploadSuccess = ({ cvId, onClose }) => {
  const navigate = useNavigate();

  return (
    <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center p-4 z-50">
      <div className="bg-white rounded-lg shadow-xl max-w-md w-full p-6 animate-fade-in">
        <div className="text-center">
          <div className="mx-auto flex items-center justify-center h-16 w-16 rounded-full bg-green-100 mb-4">
            <svg className="h-10 w-10 text-green-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M5 13l4 4L19 7" />
            </svg>
          </div>
          
          <h3 className="text-2xl font-bold text-gray-900 mb-2">CV Uploaded Successfully!</h3>
          <p className="text-gray-600 mb-6">
            Your CV is being analyzed by our AI. This usually takes 10-15 seconds.
          </p>

          <div className="space-y-3">
            <button
              onClick={() => navigate(`/cv-feedback/${cvId}`)}
              className="w-full px-6 py-3 bg-blue-600 text-white rounded-lg hover:bg-blue-700 transition-colors font-medium flex items-center justify-center"
            >
              <span className="mr-2">??</span>
              View Interactive Feedback
            </button>

            <button
              onClick={() => navigate('/cv-progress')}
              className="w-full px-6 py-3 bg-purple-600 text-white rounded-lg hover:bg-purple-700 transition-colors font-medium flex items-center justify-center"
            >
              <span className="mr-2">??</span>
              View Progress Dashboard
            </button>

            <button
              onClick={onClose}
              className="w-full px-6 py-3 bg-gray-200 text-gray-800 rounded-lg hover:bg-gray-300 transition-colors font-medium"
            >
              Upload Another CV
            </button>
          </div>

          <div className="mt-6 bg-blue-50 border-l-4 border-blue-500 p-4 text-left">
            <p className="text-sm text-blue-900">
              <strong>?? Tip:</strong> View your interactive feedback to see section-by-section scores and get personalized improvement suggestions!
            </p>
          </div>
        </div>
      </div>
    </div>
  );
};

export default CVUploadSuccess;