import React, { useState } from 'react';
import ICONS from '../utils/icons';

const ImprovementList = ({ priorities, onComplete, cvId }) => {
  const [completed, setCompleted] = useState(new Set());
  const [expandedItems, setExpandedItems] = useState(new Set());

  const handleToggle = async (index) => {
    const newCompleted = new Set(completed);
    if (newCompleted.has(index)) {
      newCompleted.delete(index);
    } else {
      newCompleted.add(index);
      if (onComplete) {
        await onComplete(cvId, index);
      }
    }
    setCompleted(newCompleted);
  };

  const toggleExpand = (index) => {
    const next = new Set(expandedItems);
    next.has(index) ? next.delete(index) : next.add(index);
    setExpandedItems(next);
  };

  const getPriorityColor = (priority) => {
    switch (priority?.toLowerCase()) {
      case 'high':
        return 'bg-red-100 text-red-800 border-red-300';
      case 'medium':
        return 'bg-yellow-100 text-yellow-800 border-yellow-300';
      case 'low':
        return 'bg-blue-100 text-blue-800 border-blue-300';
      default:
        return 'bg-gray-100 text-gray-800 border-gray-300';
    }
  };

  const getPriorityIcon = (priority) => {
    switch (priority?.toLowerCase()) {
      case 'high':
        return ICONS.warning; // high urgency
      case 'medium':
        return ICONS.lightbulb; // suggestion
      case 'low':
        return ICONS.thumbsUp; // minor tweak
      default:
        return ICONS.goal;
    }
  };

  if (!priorities || priorities.length === 0) {
    return (
      <div className="bg-white rounded-xl shadow-md p-6">
        <h2 className="text-xl font-bold text-gray-900 mb-4 flex items-center gap-2">
          <span>{ICONS.approved}</span>
          Great Job!
        </h2>
        <p className="text-green-700 bg-green-50 p-4 rounded-lg border border-green-200">
          No improvement actions needed at this time. Your CV looks great!
        </p>
      </div>
    );
  }

  const completedCount = completed.size;
  const totalCount = priorities.length;
  const progressPercentage = totalCount > 0 ? (completedCount / totalCount) * 100 : 0;

  return (
    <div className="bg-white rounded-xl shadow-md p-6">
      <div className="mb-6">
        <h2 className="text-xl font-bold text-gray-900 mb-2 flex items-center gap-2">
          <span>{ICONS.goal}</span>
          Action Plan
        </h2>
        <div className="flex items-center justify-between text-sm text-gray-600 mb-2">
          <span>{completedCount} of {totalCount} completed</span>
          <span className="font-semibold">{Math.round(progressPercentage)}%</span>
        </div>
        <div className="w-full bg-gray-200 rounded-full h-2">
          <div 
            className="bg-gradient-to-r from-blue-500 to-green-500 h-2 rounded-full transition-all duration-500"
            style={{ width: `${progressPercentage}%` }}
          />
        </div>
      </div>

      <div className="space-y-3">
        {priorities.map((item, index) => {
          const isCompleted = completed.has(index);
          const isExpanded = expandedItems.has(index);
          const priority = item.priority || item.Priority || 'Medium';
          const section = item.section || item.Section || '';
          const action = item.action || item.Action || '';
          const reason = item.reason || item.Reason || '';
          const example = item.example || item.Example || '';

          return (
            <div 
              key={index}
              className={`border-2 rounded-lg transition-all ${
                isCompleted 
                  ? 'bg-green-50 border-green-300 opacity-75' 
                  : 'bg-white border-gray-200'
              }`}
            >
              <div className="p-4">
                <div className="flex items-start gap-3">
                  <div className="flex-shrink-0 mt-1">
                    <input
                      type="checkbox"
                      checked={isCompleted}
                      onChange={() => handleToggle(index)}
                      className="w-5 h-5 text-blue-600 rounded focus:ring-2 focus:ring-blue-500 cursor-pointer"
                    />
                  </div>
                  <div className="flex-1">
                    <div className="flex items-center gap-2 mb-2">
                      <span className="text-lg" aria-hidden>{getPriorityIcon(priority)}</span>
                      <span className={`text-xs font-semibold px-2 py-1 rounded-full border ${getPriorityColor(priority)}`}>
                        {priority}
                      </span>
                      {section && (
                        <span className="text-xs font-medium text-gray-600 bg-gray-100 px-2 py-1 rounded">
                          {section}
                        </span>
                      )}
                    </div>
                    <p className={`font-medium mb-1 ${isCompleted ? 'line-through text-gray-500' : 'text-gray-900'}`}>
                      {action}
                    </p>
                    {reason && (
                      <p className="text-sm text-gray-600 italic mb-2">
                        {ICONS.lightbulb} {reason}
                      </p>
                    )}
                    {example && (
                      <button
                        onClick={() => toggleExpand(index)}
                        className="text-sm text-blue-600 hover:text-blue-800 font-medium flex items-center gap-1"
                      >
                        {isExpanded ? '?' : ICONS.next} {isExpanded ? 'Hide' : 'View'} Example
                      </button>
                    )}
                  </div>
                </div>
              </div>
              
              {/* Expandable Example Section */}
              {example && isExpanded && (
                <div className="px-4 pb-4">
                  <div className="bg-blue-50 border-l-4 border-blue-500 p-4 rounded-r-lg">
                    <h4 className="text-sm font-semibold text-blue-900 mb-2 flex items-center gap-2">
                      <span>{ICONS.document}</span>
                      Example
                    </h4>
                    <div className="text-sm text-blue-800 whitespace-pre-line">
                      {example}
                    </div>
                  </div>
                </div>
              )}
            </div>
          );
        })}
      </div>

      {completedCount === totalCount && totalCount > 0 && (
        <div className="mt-4 p-4 bg-gradient-to-r from-green-50 to-blue-50 border-2 border-green-300 rounded-lg">
          <p className="text-green-800 font-semibold text-center flex items-center justify-center gap-2">
            <span>{ICONS.celebrate}</span>
            All actions completed! Ready to upload improved version.
            <span>{ICONS.celebrate}</span>
          </p>
        </div>
      )}
    </div>
  );
};

export default ImprovementList;