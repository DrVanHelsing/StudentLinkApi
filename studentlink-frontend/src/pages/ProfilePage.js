import React, { useState, useEffect } from 'react';
import { useAuth } from '../contexts/AuthContext';
import { authAPI } from '../services/api';

export const ProfilePage = () => {
  const { user } = useAuth();
  const [editing, setEditing] = useState(false);
  const [formData, setFormData] = useState({
    firstName: '',
    lastName: '',
    phoneNumber: ''
  });
  const [message, setMessage] = useState('');
  const [loading, setLoading] = useState(false);

  useEffect(() => {
    if (user) {
      setFormData({
        firstName: user.firstName || '',
        lastName: user.lastName || '',
        phoneNumber: user.phoneNumber || ''
      });
    }
  }, [user]);

  const handleChange = (e) => {
    setFormData({
      ...formData,
      [e.target.name]: e.target.value
    });
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    setLoading(true);
    setMessage('');

    try {
      await authAPI.updateProfile(formData);
      setMessage('Profile updated successfully!');
      setEditing(false);
      window.location.reload(); // Reload to get updated user data
    } catch (error) {
      setMessage('Failed to update profile. Please try again.');
    } finally {
      setLoading(false);
    }
  };

  // Layout chrome (Navbar, container) provided by WithNav wrapper
  return (
    <div className="max-w-2xl mx-auto">
      <div className="bg-white rounded-lg shadow-lg overflow-hidden">
        <div className="bg-gradient-to-r from-blue-500 to-purple-600 p-8 text-white">
          <div className="flex items-center space-x-4">
            <div className="w-20 h-20 bg-white rounded-full flex items-center justify-center text-4xl font-bold text-blue-600">
              {user?.firstName?.[0] || user?.email?.[0].toUpperCase()}
            </div>
            <div>
              <h1 className="text-3xl font-bold">{user?.firstName} {user?.lastName}</h1>
              <p className="text-blue-100">{user?.email}</p>
            </div>
          </div>
        </div>

        <div className="p-8">
          {message && (
            <div className="mb-4 p-4 rounded bg-green-50 text-green-700 border border-green-200">
              {message}
            </div>
          )}

          {!editing ? (
            <div className="space-y-4">
              <div>
                <div className="text-sm text-gray-500">First Name</div>
                <div className="text-gray-900 font-medium">{formData.firstName || '-'}</div>
              </div>
              <div>
                <div className="text-sm text-gray-500">Last Name</div>
                <div className="text-gray-900 font-medium">{formData.lastName || '-'}</div>
              </div>
              <div>
                <div className="text-sm text-gray-500">Phone Number</div>
                <div className="text-gray-900 font-medium">{formData.phoneNumber || '-'}</div>
              </div>
              <div className="pt-4">
                <button onClick={() => setEditing(true)} className="px-6 py-3 bg-blue-600 hover:bg-blue-700 text-white rounded-lg transition">
                  Edit Profile
                </button>
              </div>
            </div>
          ) : (
            <form onSubmit={handleSubmit} className="space-y-4">
              <div>
                <label className="block text-sm text-gray-700 mb-1">First Name</label>
                <input
                  type="text"
                  name="firstName"
                  value={formData.firstName}
                  onChange={handleChange}
                  className="w-full border border-gray-300 rounded-lg p-3 focus:ring-2 focus:ring-blue-500 focus:border-transparent"
                />
              </div>
              <div>
                <label className="block text-sm text-gray-700 mb-1">Last Name</label>
                <input
                  type="text"
                  name="lastName"
                  value={formData.lastName}
                  onChange={handleChange}
                  className="w-full border border-gray-300 rounded-lg p-3 focus:ring-2 focus:ring-blue-500 focus:border-transparent"
                />
              </div>
              <div>
                <label className="block text-sm text-gray-700 mb-1">Phone Number</label>
                <input
                  type="text"
                  name="phoneNumber"
                  value={formData.phoneNumber}
                  onChange={handleChange}
                  className="w-full border border-gray-300 rounded-lg p-3 focus:ring-2 focus:ring-blue-500 focus:border-transparent"
                />
              </div>
              <div className="pt-4 flex gap-3">
                <button type="submit" disabled={loading} className="px-6 py-3 bg-green-600 hover:bg-green-700 text-white rounded-lg transition disabled:opacity-50">
                  {loading ? 'Saving...' : 'Save Changes'}
                </button>
                <button type="button" onClick={() => setEditing(false)} className="px-6 py-3 bg-gray-200 hover:bg-gray-300 rounded-lg transition">
                  Cancel
                </button>
              </div>
            </form>
          )}
        </div>
      </div>
    </div>
  );
};