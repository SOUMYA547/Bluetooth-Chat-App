import React, { useState, useEffect } from 'react';
import { SplashScreen } from './components/SplashScreen';
import { OnboardingFlow } from './components/OnboardingFlow';
import { AuthScreen } from './components/AuthScreen';
import { Dashboard } from './components/Dashboard';
import { DeviceDiscoveryModal } from './components/DeviceDiscoveryModal';
import { PermissionModal } from './components/PermissionModal';

export type AppScreen = 'splash' | 'onboarding' | 'auth' | 'dashboard';

export interface User {
  id: string;
  name: string;
  avatar?: string;
  status: 'online' | 'away' | 'busy' | 'offline';
}

export interface ChatDevice {
  id: string;
  name: string;
  distance: number;
  lastSeen: Date;
  isConnected: boolean;
  unreadCount: number;
}

export interface Message {
  id: string;
  deviceId: string;
  content: string;
  timestamp: Date;
  isSent: boolean;
  hasAttachment?: boolean;
  attachmentType?: 'image' | 'file';
}

export default function App() {
  const [currentScreen, setCurrentScreen] = useState<AppScreen>('splash');
  const [isDarkMode, setIsDarkMode] = useState(true);
  const [isBluetoothEnabled, setIsBluetoothEnabled] = useState(true);
  const [showDeviceDiscovery, setShowDeviceDiscovery] = useState(false);
  const [showPermissionModal, setShowPermissionModal] = useState(false);
  const [permissionType, setPermissionType] = useState<'bluetooth' | 'location' | 'notifications'>('bluetooth');
  
  const [user, setUser] = useState<User>({
    id: '1',
    name: 'Alex Johnson',
    avatar: 'https://images.unsplash.com/photo-1472099645785-5658abf4ff4e?w=150&h=150&fit=crop&crop=face',
    status: 'online'
  });

  const [devices, setDevices] = useState<ChatDevice[]>([
    {
      id: '1',
      name: 'Sarah\'s iPhone',
      distance: 2.5,
      lastSeen: new Date(Date.now() - 300000),
      isConnected: true,
      unreadCount: 3
    },
    {
      id: '2', 
      name: 'Mike\'s MacBook',
      distance: 8.1,
      lastSeen: new Date(Date.now() - 900000),
      isConnected: false,
      unreadCount: 0
    },
    {
      id: '3',
      name: 'Emma\'s Galaxy',
      distance: 15.3,
      lastSeen: new Date(Date.now() - 1800000),
      isConnected: true,
      unreadCount: 1
    }
  ]);

  const [messages, setMessages] = useState<Message[]>([
    {
      id: '1',
      deviceId: '1',
      content: 'Hey! Are you still in the coffee shop?',
      timestamp: new Date(Date.now() - 600000),
      isSent: false
    },
    {
      id: '2',
      deviceId: '1', 
      content: 'Yes, just finishing up some work',
      timestamp: new Date(Date.now() - 300000),
      isSent: true
    },
    {
      id: '3',
      deviceId: '1',
      content: 'Perfect! I\'ll be there in 5 minutes',
      timestamp: new Date(Date.now() - 120000),
      isSent: false
    }
  ]);

  useEffect(() => {
    // Simulate splash screen timing
    if (currentScreen === 'splash') {
      const timer = setTimeout(() => {
        setCurrentScreen('onboarding');
      }, 2000);
      return () => clearTimeout(timer);
    }
  }, [currentScreen]);

  useEffect(() => {
    // Always apply dark mode class to document
    document.documentElement.classList.add('dark');
  }, []);

  const handleOnboardingComplete = () => {
    setCurrentScreen('auth');
  };

  const handleAuthComplete = () => {
    setCurrentScreen('dashboard');
    // Show permission modal for first-time users
    setPermissionType('bluetooth');
    setShowPermissionModal(true);
  };

  const handlePermissionGranted = () => {
    setShowPermissionModal(false);
    if (permissionType === 'bluetooth') {
      setIsBluetoothEnabled(true);
    }
  };

  const handleSendMessage = (content: string, deviceId: string) => {
    const newMessage: Message = {
      id: Date.now().toString(),
      deviceId,
      content,
      timestamp: new Date(),
      isSent: true
    };
    setMessages(prev => [...prev, newMessage]);
  };

  const handleUpdateUser = (updatedUser: User) => {
    setUser(updatedUser);
  };

  // Dark mode is always enabled - no toggle functionality

  const renderCurrentScreen = () => {
    switch (currentScreen) {
      case 'splash':
        return <SplashScreen />;
      case 'onboarding':
        return <OnboardingFlow onComplete={handleOnboardingComplete} />;
      case 'auth':
        return <AuthScreen onComplete={handleAuthComplete} />;
      case 'dashboard':
        return (
          <Dashboard
            user={user}
            devices={devices}
            messages={messages}
            isBluetoothEnabled={isBluetoothEnabled}
            isDarkMode={isDarkMode}
            onSendMessage={handleSendMessage}
            onToggleDarkMode={() => {}} // Dark mode always enabled
            onShowDeviceDiscovery={() => setShowDeviceDiscovery(true)}
            onShowPermissionModal={(type) => {
              setPermissionType(type);
              setShowPermissionModal(true);
            }}
            onUpdateUser={handleUpdateUser}
          />
        );
      default:
        return <SplashScreen />;
    }
  };

  return (
    <div className="size-full bg-background text-foreground">
      {renderCurrentScreen()}
      
      {showDeviceDiscovery && (
        <DeviceDiscoveryModal
          onClose={() => setShowDeviceDiscovery(false)}
          onDeviceSelect={(device) => {
            setDevices(prev => [...prev, device]);
            setShowDeviceDiscovery(false);
          }}
        />
      )}

      {showPermissionModal && (
        <PermissionModal
          type={permissionType}
          onAllow={handlePermissionGranted}
          onDeny={() => setShowPermissionModal(false)}
        />
      )}
    </div>
  );
}