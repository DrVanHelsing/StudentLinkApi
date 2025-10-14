import React, { useState } from 'react';
import { useNavigate, Link } from 'react-router-dom';
import { useAuth } from '../contexts/AuthContext';

export const LoginPage = () => {
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(false);
  const { login } = useAuth();
  const navigate = useNavigate();

  const fill = (e, p) => { setEmail(e); setPassword(p); };

  const handleSubmit = async (e) => {
    e.preventDefault();
    setError('');
    setLoading(true);

    try {
      await login(email, password);
      navigate('/dashboard');
    } catch (err) {
      setError(err.response?.data?.error || 'Login failed. Please check your credentials.');
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="min-h-screen bg-gradient-to-br from-blue-500 to-purple-600 flex items-center justify-center px-4">
      <div className="max-w-md w-full bg-white rounded-lg shadow-xl p-8">
        <div className="text-center mb-8">
          <h1 className="text-3xl font-bold text-gray-800">StudentLink</h1>
          <p className="text-gray-600 mt-2">Sign in to your account</p>
        </div>

        {error && (
          <div className="mb-4 p-4 bg-red-100 border border-red-400 text-red-700 rounded">
            {error}
          </div>
        )}

        <form onSubmit={handleSubmit} className="space-y-6">
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-2">
              Email Address
            </label>
            <input
              type="email"
              required
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              className="w-full px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-transparent"
              placeholder="student@example.com"
            />
          </div>

          <div>
            <label className="block text-sm font-medium text-gray-700 mb-2">
              Password
            </label>
            <input
              type="password"
              required
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              className="w-full px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-transparent"
              placeholder="Enter your password"
              autoComplete="current-password"
            />
          </div>

          <button
            type="submit"
            disabled={loading}
            className="w-full py-3 px-4 bg-blue-600 hover:bg-blue-700 text-white font-medium rounded-lg transition disabled:opacity-50 disabled:cursor-not-allowed"
          >
            {loading ? 'Signing in...' : 'Sign In'}
          </button>
        </form>

        <div className="mt-6 text-center">
          <p className="text-gray-600">
            Don't have an account?{' '}
            <Link to="/register" className="text-blue-600 hover:text-blue-700 font-medium">
              Sign up
            </Link>
          </p>
        </div>

        {/* Quick sample accounts */}
        <div className="mt-8 pt-6 border-t border-gray-200">
          <p className="text-xs text-gray-500 text-center mb-3">Quick Login (Sample Accounts)</p>
          <div className="grid grid-cols-2 gap-2 mb-4">
            <button onClick={() => fill('admin@studentlink.com', 'Admin123!')} className="text-xs px-2 py-2 bg-red-50 hover:bg-red-100 rounded border border-red-200">
              Admin
              <div className="mt-1 text-[10px] text-red-600 break-all">admin@studentlink.com / Admin123!</div>
            </button>
            <button onClick={() => fill('recruiter1@techcorp.com', 'Recruiter123!')} className="text-xs px-2 py-2 bg-purple-50 hover:bg-purple-100 rounded border border-purple-200">
              Recruiter 1
              <div className="mt-1 text-[10px] text-purple-600 break-all">recruiter1@techcorp.com / Recruiter123!</div>
            </button>
            <button onClick={() => fill('recruiter2@innovate.com', 'Recruiter123!')} className="text-xs px-2 py-2 bg-purple-50 hover:bg-purple-100 rounded border border-purple-200">
              Recruiter 2
              <div className="mt-1 text-[10px] text-purple-600 break-all">recruiter2@innovate.com / Recruiter123!</div>
            </button>
            <button onClick={() => fill('john.doe@student.com', 'Student123!')} className="text-xs px-2 py-2 bg-blue-50 hover:bg-blue-100 rounded border border-blue-200">
              Student John
              <div className="mt-1 text-[10px] text-blue-600 break-all">john.doe@student.com / Student123!</div>
            </button>
            <button onClick={() => fill('jane.smith@student.com', 'Student123!')} className="text-xs px-2 py-2 bg-blue-50 hover:bg-blue-100 rounded border border-blue-200">
              Student Jane
              <div className="mt-1 text-[10px] text-blue-600 break-all">jane.smith@student.com / Student123!</div>
            </button>
            <button onClick={() => fill('alex.wong@student.com', 'Student123!')} className="text-xs px-2 py-2 bg-blue-50 hover:bg-blue-100 rounded border border-blue-200">
              Student Alex
              <div className="mt-1 text-[10px] text-blue-600 break-all">alex.wong@student.com / Student123!</div>
            </button>
            <button onClick={() => fill('emily.brown@student.com', 'Student123!')} className="text-xs px-2 py-2 bg-blue-50 hover:bg-blue-100 rounded border border-blue-200">
              Student Emily
              <div className="mt-1 text-[10px] text-blue-600 break-all">emily.brown@student.com / Student123!</div>
            </button>
            <button onClick={() => fill('david.lee@student.com', 'Student123!')} className="text-xs px-2 py-2 bg-blue-50 hover:bg-blue-100 rounded border border-blue-200">
              Student David
              <div className="mt-1 text-[10px] text-blue-600 break-all">david.lee@student.com / Student123!</div>
            </button>
          </div>
          <p className="text-[10px] text-center text-gray-400">Click a box to auto-fill credentials, then press Sign In.</p>
        </div>
      </div>
    </div>
  );
};