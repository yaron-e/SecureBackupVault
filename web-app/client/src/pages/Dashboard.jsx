import React, { useState, useEffect, useContext } from 'react';
import FilesList from '../components/FilesList';
import { AuthContext } from '../context/AuthContext';
import { getUserStats } from '../services/api';

const Dashboard = () => {
  const { user } = useContext(AuthContext);
  const [stats, setStats] = useState({
    totalFiles: 0,
    totalSize: '0 B',
    lastBackup: 'Never'
  });
  const [loading, setLoading] = useState(true);

  // Fetch user stats
  useEffect(() => {
    const fetchStats = async () => {
      try {
        setLoading(true);
        const data = await getUserStats();
        setStats(data);
      } catch (error) {
        console.error('Error fetching user stats:', error);
        // Keep default stats on error
      } finally {
        setLoading(false);
      }
    };

    fetchStats();
  }, []);

  // Format date in a user-friendly way
  const formatDate = (dateString) => {
    if (!dateString || dateString === 'Never') return 'Never';
    
    const options = { 
      year: 'numeric', 
      month: 'short', 
      day: 'numeric',
      hour: '2-digit',
      minute: '2-digit'
    };
    return new Date(dateString).toLocaleDateString(undefined, options);
  };

  return (
    <div className="space-y-6">
      {/* Welcome Banner */}
      <div className="bg-gradient-to-r from-blue-600 to-blue-800 rounded-lg shadow-lg p-6 text-white">
        <h1 className="text-2xl font-bold">Welcome, {user?.name || user?.email.split('@')[0] || 'User'}</h1>
        <p className="mt-2 opacity-90">Manage your encrypted backups securely from this dashboard.</p>
      </div>

      {/* Stats Cards */}
      <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
        <div className="bg-white rounded-lg shadow-md p-5">
          <div className="flex items-center">
            <div className="p-3 rounded-full bg-blue-100 text-blue-500 mr-4">
              <svg xmlns="http://www.w3.org/2000/svg" className="h-6 w-6" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M5 8h14M5 8a2 2 0 110-4h14a2 2 0 110 4M5 8v10a2 2 0 002 2h10a2 2 0 002-2V8m-9 4h4" />
              </svg>
            </div>
            <div>
              <p className="text-gray-500 text-sm uppercase">Total Files</p>
              <p className="text-gray-800 text-xl font-semibold">
                {loading ? (
                  <span className="inline-block w-12 h-6 bg-gray-200 animate-pulse rounded"></span>
                ) : (
                  stats.totalFiles
                )}
              </p>
            </div>
          </div>
        </div>
        
        <div className="bg-white rounded-lg shadow-md p-5">
          <div className="flex items-center">
            <div className="p-3 rounded-full bg-green-100 text-green-500 mr-4">
              <svg xmlns="http://www.w3.org/2000/svg" className="h-6 w-6" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M4 7v10c0 2.21 3.582 4 8 4s8-1.79 8-4V7M4 7c0 2.21 3.582 4 8 4s8-1.79 8-4M4 7c0-2.21 3.582-4 8-4s8 1.79 8 4" />
              </svg>
            </div>
            <div>
              <p className="text-gray-500 text-sm uppercase">Total Storage</p>
              <p className="text-gray-800 text-xl font-semibold">
                {loading ? (
                  <span className="inline-block w-16 h-6 bg-gray-200 animate-pulse rounded"></span>
                ) : (
                  stats.totalSize
                )}
              </p>
            </div>
          </div>
        </div>
        
        <div className="bg-white rounded-lg shadow-md p-5">
          <div className="flex items-center">
            <div className="p-3 rounded-full bg-purple-100 text-purple-500 mr-4">
              <svg xmlns="http://www.w3.org/2000/svg" className="h-6 w-6" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z" />
              </svg>
            </div>
            <div>
              <p className="text-gray-500 text-sm uppercase">Last Backup</p>
              <p className="text-gray-800 text-xl font-semibold">
                {loading ? (
                  <span className="inline-block w-24 h-6 bg-gray-200 animate-pulse rounded"></span>
                ) : (
                  formatDate(stats.lastBackup)
                )}
              </p>
            </div>
          </div>
        </div>
      </div>

      {/* Main Content */}
      <FilesList />

      {/* Info Cards */}
      <div className="grid grid-cols-1 md:grid-cols-2 gap-6 mt-6">
        <div className="bg-white rounded-lg shadow-md p-6 border-l-4 border-blue-500">
          <h3 className="text-lg font-semibold text-gray-800 mb-2">Encryption Security</h3>
          <p className="text-gray-600 mb-4">
            Your files are protected with triple-layer cascade encryption:
          </p>
          <ul className="list-disc list-inside text-gray-600 space-y-1">
            <li>AES-256 (first layer)</li>
            <li>Twofish (second layer)</li>
            <li>Serpent (third layer)</li>
          </ul>
          <p className="text-gray-600 mt-4">
            This provides military-grade security for your sensitive data.
          </p>
        </div>
        
        <div className="bg-white rounded-lg shadow-md p-6 border-l-4 border-green-500">
          <h3 className="text-lg font-semibold text-gray-800 mb-2">Windows Application</h3>
          <p className="text-gray-600 mb-4">
            To back up new files, you'll need to use the Windows application:
          </p>
          <ul className="list-disc list-inside text-gray-600 space-y-1">
            <li>Select files and folders to back up</li>
            <li>Configure automatic backups on file changes</li>
            <li>Set file type filters</li>
          </ul>
          <p className="text-gray-600 mt-4">
            The web interface is for managing your existing backups only.
          </p>
        </div>
      </div>
    </div>
  );
};

export default Dashboard;
