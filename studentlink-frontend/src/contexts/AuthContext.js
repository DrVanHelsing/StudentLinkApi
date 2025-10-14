import React, { createContext, useState, useContext, useEffect, useCallback } from 'react';
import { authAPI } from '../services/api';

const AuthContext = createContext(null);

export const AuthProvider = ({ children }) => {
  const [user, setUser] = useState(null);
  const [token, setToken] = useState(localStorage.getItem('token'));
  const [loading, setLoading] = useState(true);
  const [loadError, setLoadError] = useState(null);

  const loadUser = useCallback(async () => {
    setLoadError(null);
    try {
      const response = await authAPI.getCurrentUser();
      setUser(response.data);
    } catch (error) {
      console.error('Failed to load user:', error);
      setLoadError(error.response?.data?.error || 'Failed to validate session');
      // Do NOT auto logout on 500; only clear on 401/403
      const status = error.response?.status;
      if (status === 401 || status === 403) {
        logout();
      }
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    if (token) {
      loadUser();
    } else {
      setLoading(false);
    }
  }, [token, loadUser]);

  const persistAuth = (rawToken, rawUser) => {
    localStorage.setItem('token', rawToken);
    localStorage.setItem('user', JSON.stringify(rawUser));
    setToken(rawToken);
    setUser(rawUser);
  };

  const normalizeResponse = (data) => {
    const tokenValue = data.token || data.Token;
    const userObj = data.user || data.User;
    return { token: tokenValue, user: userObj };
  };

  const login = async (email, password) => {
    const response = await authAPI.login({ email, password });
    const { token: t, user: u } = normalizeResponse(response.data);
    if (!t || !u) throw new Error('Invalid login response');
    persistAuth(t, u);
    return u;
  };

  const register = async (userData) => {
    const response = await authAPI.register(userData);
    const { token: t, user: u } = normalizeResponse(response.data);
    if (!t || !u) throw new Error('Invalid register response');
    persistAuth(t, u);
    return u;
  };

  const logout = () => {
    localStorage.removeItem('token');
    localStorage.removeItem('user');
    setToken(null);
    setUser(null);
  };

  const value = {
    user,
    token,
    loading,
    loadError,
    login,
    register,
    logout,
    isAuthenticated: !!token
  };

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
};

export const useAuth = () => {
  const context = useContext(AuthContext);
  if (!context) throw new Error('useAuth must be used within AuthProvider');
  return context;
};