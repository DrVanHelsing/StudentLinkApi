import React, { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { useAuth } from '../contexts/AuthContext';
import api from '../services/api';
import CVUploadSuccess from '../components/CVUploadSuccess';
import ICONS from '../utils/icons';

export const CVUploadPage = () => {
  const { user } = useAuth();
  const navigate = useNavigate();
  const [currentCV, setCurrentCV] = useState(null);
  const [cvHistory, setCVHistory] = useState([]);
  const [uploading, setUploading] = useState(false);
  const [message, setMessage] = useState('');
  const [dragActive, setDragActive] = useState(false);
  const [showSuccessModal, setShowSuccessModal] = useState(false);
  const [uploadedCvId, setUploadedCvId] = useState(null);

  useEffect(() => {
    loadCurrentCV();
    loadCVHistory();
  }, []);

  const loadCurrentCV = async () => {
    try {
      const response = await api.get('/cv/current');
      setCurrentCV(response.data.hasCV ? response.data : null);
    } catch (error) {
      if (error.response?.status !== 404) {
        console.error('Error loading CV:', error);
      }
    }
  };

  const loadCVHistory = async () => {
    try {
      const response = await api.get('/cv/history');
      setCVHistory(response.data);
    } catch (error) {
      console.error('Error loading CV history:', error);
    }
  };

  const handleFileUpload = async (file) => {
    if (!file) return;
    const allowedTypes = ['.pdf', '.doc', '.docx'];
    const fileExtension = file.name.substring(file.name.lastIndexOf('.')).toLowerCase();
    if (!allowedTypes.includes(fileExtension)) {
      setMessage('Invalid file type. Only PDF, DOC, and DOCX files are allowed.');
      return;
    }
    if (file.size > 5 * 1024 * 1024) {
      setMessage('File size exceeds 5MB limit.');
      return;
    }

    setUploading(true);
    setMessage('');
    try {
      const formData = new FormData();
      formData.append('file', file);
      const response = await api.post('/cv/upload', formData, { headers: { 'Content-Type': 'multipart/form-data' } });
      setUploadedCvId(response.data.cv.id);
      setShowSuccessModal(true);
      loadCurrentCV();
      loadCVHistory();
    } catch (error) {
      setMessage(error.response?.data?.error || 'Failed to upload CV');
    } finally {
      setUploading(false);
    }
  };

  const handleDrag = (e) => {
    e.preventDefault();
    e.stopPropagation();
    if (e.type === 'dragenter' || e.type === 'dragover') setDragActive(true);
    else if (e.type === 'dragleave') setDragActive(false);
  };

  const handleDrop = (e) => {
    e.preventDefault();
    e.stopPropagation();
    setDragActive(false);
    if (e.dataTransfer.files && e.dataTransfer.files[0]) handleFileUpload(e.dataTransfer.files[0]);
  };

  const handleFileChange = (e) => {
    if (e.target.files && e.target.files[0]) handleFileUpload(e.target.files[0]);
  };

  const handleDownload = async (cvId, fileName) => {
    try {
      const response = await api.get(`/cv/download/${cvId}`, { responseType: 'blob' });
      const url = window.URL.createObjectURL(new Blob([response.data]));
      const link = document.createElement('a');
      link.href = url; link.setAttribute('download', fileName); document.body.appendChild(link); link.click(); link.remove();
    } catch { setMessage('Failed to download CV'); }
  };

  const handleDelete = async (cvId) => {
    if (!window.confirm('Are you sure you want to delete this CV?')) return;
    try {
      await api.delete(`/cv/${cvId}`); setMessage('CV deleted successfully'); setCurrentCV(null); loadCVHistory();
    } catch { setMessage('Failed to delete CV'); }
  };

  const formatFileSize = (bytes) => bytes < 1024 ? bytes + ' B' : bytes < 1048576 ? (bytes/1024).toFixed(2) + ' KB' : (bytes/1048576).toFixed(2) + ' MB';
  const formatDate = (dateString) => new Date(dateString).toLocaleDateString('en-US', {year:'numeric',month:'short',day:'numeric',hour:'2-digit',minute:'2-digit'});

  return (
    <div className="max-w-4xl mx-auto">
      <div className="bg-white rounded-lg shadow p-6 mb-6">
        <h1 className="text-2xl font-bold text-gray-800 mb-2 flex items-center gap-2">{ICONS.upload} Upload Your CV</h1>
        <p className="text-gray-600 mb-6">Upload your CV to get AI-powered feedback and improve your chances of landing your dream job!</p>

        {message && (
          <div className={`p-4 mb-4 rounded-lg ${message.includes('success') ? 'bg-green-100 text-green-800' : 'bg-red-100 text-red-800'}`}>{message}</div>
        )}

        <div className="flex gap-3 mb-6">
          <button onClick={() => navigate('/cv-progress')} className="flex items-center px-4 py-2 bg-purple-600 text-white rounded-lg hover:bg-purple-700 transition-colors">
            <span className="mr-2">{ICONS.chart}</span>View Progress
          </button>
          {currentCV && (
            <button onClick={() => navigate(`/cv-feedback/${currentCV.id}`)} className="flex items-center px-4 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700 transition-colors">
              <span className="mr-2">{ICONS.document}</span>View Feedback
            </button>
          )}
        </div>

        <div className={`border-2 border-dashed rounded-lg p-8 text-center transition ${dragActive ? 'border-blue-500 bg-blue-50' : 'border-gray-300 hover:border-blue-400'}`}
             onDragEnter={handleDrag} onDragLeave={handleDrag} onDragOver={handleDrag} onDrop={handleDrop}>
          <div className="mb-4">
            <div className="text-6xl mb-4">{ICONS.cv}</div>
            <p className="text-lg text-gray-700 mb-2">Drag and drop your CV here, or click to browse</p>
            <input id="cv-upload" type="file" accept=".pdf,.doc,.docx" onChange={handleFileChange} className="hidden" disabled={uploading} />
            <label htmlFor="cv-upload" className="inline-block px-6 py-3 bg-blue-600 text-white font-medium rounded-lg hover:bg-blue-700 cursor-pointer transition disabled:opacity-50">{uploading ? 'Uploading...' : 'Choose File'}</label>
            <p className="text-xs text-gray-500 mt-4">Accepted formats: PDF, DOC, DOCX (Max 5MB)</p>
          </div>
        </div>
      </div>

      {currentCV && (
        <div className="bg-white rounded-lg shadow p-6 mb-6">
          <div className="flex items-center justify-between mb-4">
            <h2 className="text-xl font-semibold flex items-center gap-2">{ICONS.document} Current CV</h2>
            {currentCV.qualityScore && (
              <div className="flex items-center space-x-2">
                <span className="text-sm text-gray-600">Score:</span>
                <span className={`px-3 py-1 rounded-full text-sm font-semibold ${currentCV.qualityScore>=0.8?'bg-green-100 text-green-800':currentCV.qualityScore>=0.6?'bg-yellow-100 text-yellow-800':'bg-red-100 text-red-800'}`}>{Math.round(currentCV.qualityScore*100)}%</span>
                {currentCV.isApproved && <span className="text-green-600">{ICONS.approved}</span>}
              </div>
            )}
          </div>
          <div className="flex items-center justify-between p-4 bg-gray-50 rounded-lg">
            <div className="flex items-center space-x-4">
              <div className="text-4xl">{ICONS.cv}</div>
              <div>
                <p className="font-medium text-gray-800">{currentCV.fileName}</p>
                <p className="text-sm text-gray-500">{formatFileSize(currentCV.fileSize)} • Uploaded {formatDate(currentCV.uploadedAt)}</p>
              </div>
            </div>
            <div className="flex space-x-2">
              <button onClick={()=>navigate(`/cv-feedback/${currentCV.id}`)} className="px-4 py-2 bg-blue-600 text-white rounded hover:bg-blue-700 transition">View Feedback</button>
              <button onClick={()=>handleDownload(currentCV.id,currentCV.fileName)} className="px-4 py-2 bg-gray-600 text-white rounded hover:bg-gray-700 transition">Download</button>
              <button onClick={()=>handleDelete(currentCV.id)} className="px-4 py-2 bg-red-600 text-white rounded hover:bg-red-700 transition">Delete</button>
            </div>
          </div>
        </div>
      )}

      {cvHistory.length > 0 && (
        <div className="bg-white rounded-lg shadow p-6">
          <h2 className="text-xl font-semibold mb-4 flex items-center gap-2">{ICONS.calendar} CV History</h2>
          <div className="space-y-3">
            {cvHistory.map(cv => (
              <div key={cv.id} className={`flex items-center justify-between p-4 rounded-lg ${cv.isActive?'bg-blue-50 border border-blue-200':'bg-gray-50'}`}>
                <div className="flex items-center space-x-4">
                  <div className="text-2xl">{ICONS.document}</div>
                  <div>
                    <div className="flex items-center space-x-2">
                      <p className="font-medium text-gray-800">{cv.fileName}</p>
                      {cv.isActive && <span className="px-2 py-1 bg-blue-600 text-white text-xs rounded">Current</span>}
                      {cv.qualityScore && <span className={`px-2 py-1 rounded text-xs font-semibold ${cv.qualityScore>=0.8?'bg-green-100 text-green-800':cv.qualityScore>=0.6?'bg-yellow-100 text-yellow-800':'bg-red-100 text-red-800'}`}>{Math.round(cv.qualityScore*100)}%</span>}
                    </div>
                    <p className="text-sm text-gray-500">{formatFileSize(cv.fileSize)} • {formatDate(cv.uploadedAt)}</p>
                  </div>
                </div>
                <div className="flex space-x-2">
                  <button onClick={()=>navigate(`/cv-feedback/${cv.id}`)} className="px-3 py-1 text-sm bg-blue-600 text-white hover:bg-blue-700 rounded transition">Feedback</button>
                  <button onClick={()=>handleDownload(cv.id,cv.fileName)} className="px-3 py-1 text-sm bg-gray-200 hover:bg-gray-300 rounded transition">Download</button>
                </div>
              </div>
            ))}
          </div>
        </div>
      )}

      {!currentCV && cvHistory.length === 0 && (
        <div className="bg-white rounded-lg shadow p-12 text-center">
          <div className="text-6xl mb-4">{ICONS.upload}</div>
          <p className="text-xl text-gray-600 mb-2">No CV uploaded yet</p>
          <p className="text-gray-500">Upload your CV to get AI-powered feedback and start improving!</p>
        </div>
      )}

      {showSuccessModal && uploadedCvId && (
        <CVUploadSuccess cvId={uploadedCvId} onClose={()=>{setShowSuccessModal(false); setUploadedCvId(null);}} />
      )}
    </div>
  );
};