import React, { useState, useEffect, useRef } from 'react';
import './App.css';

const App = () => {
  const [isRecording, setIsRecording] = useState(false);
  const [isProcessing, setIsProcessing] = useState(false);
  const [isTalking, setIsTalking] = useState(false);
  
  const mediaRecorderRef = useRef(null);
  const audioChunksRef = useRef([]);
  const audioContextRef = useRef(null);
  const socketRef = useRef(null);
  const mediaSourceRef = useRef(null);
  const sourceBufferRef = useRef(null);
  const audioElementRef = useRef(null);
  const audioQueueRef = useRef([]);
  const isSourceOpenRef = useRef(false);

  useEffect(() => {
    audioContextRef.current = new (window.AudioContext || window.webkitAudioContext)();
    
    audioElementRef.current = new Audio();
    document.body.appendChild(audioElementRef.current);
    
    audioElementRef.current.addEventListener('ended', () => {
      setIsTalking(false);
    });
    
    return () => {
      if (socketRef.current) {
        socketRef.current.close();
      }
      if (audioElementRef.current) {
        audioElementRef.current.removeEventListener('ended', () => {
          setIsTalking(false);
        });
        document.body.removeChild(audioElementRef.current);
      }
    };
  }, []);

  const startRecording = async () => {
    audioChunksRef.current = [];
    try {
      const stream = await navigator.mediaDevices.getUserMedia({ audio: true });
      const options = { mimeType: 'audio/webm;codecs=opus' };
      mediaRecorderRef.current = new MediaRecorder(stream, options);
      
      mediaRecorderRef.current.ondataavailable = (event) => {
        if (event.data.size > 0) {
          audioChunksRef.current.push(event.data);
        }
      };

      mediaRecorderRef.current.start(100);
      setIsRecording(true);
    } catch (err) {
      console.error("Error accessing microphone:", err);
    }
  };

  const stopRecording = async () => {
    if (!mediaRecorderRef.current) return;
    
    return new Promise(resolve => {
      mediaRecorderRef.current.onstop = async () => {
        setIsRecording(false);
        setIsProcessing(true);
        sendAudioToBackend();
        resolve();
      };
      
      mediaRecorderRef.current.stop();
      mediaRecorderRef.current.stream.getTracks().forEach(track => track.stop());
    });
  };
  
  const setupMediaSource = () => {
    mediaSourceRef.current = new MediaSource();
    audioElementRef.current.src = URL.createObjectURL(mediaSourceRef.current);
    
    mediaSourceRef.current.addEventListener('sourceopen', () => {
      isSourceOpenRef.current = true;
      
      try {
        sourceBufferRef.current = mediaSourceRef.current.addSourceBuffer('audio/mpeg');
        
        sourceBufferRef.current.addEventListener('updateend', () => {
          if (audioQueueRef.current.length > 0 && !sourceBufferRef.current.updating) {
            const chunk = audioQueueRef.current.shift();
            appendToSourceBuffer(chunk);
          }
        });
        
        if (audioQueueRef.current.length > 0 && !sourceBufferRef.current.updating) {
          const chunk = audioQueueRef.current.shift();
          appendToSourceBuffer(chunk);
        }
      } catch (e) {
        console.error('Error setting up MediaSource:', e);
      }
    });
    
    audioElementRef.current.play()
      .then(() => {
        setIsTalking(true);
        setIsProcessing(false);
      })
      .catch(e => console.error('Error playing audio:', e));
  };
  
  const appendToSourceBuffer = (chunk) => {
    if (!sourceBufferRef.current || sourceBufferRef.current.updating) {
      audioQueueRef.current.push(chunk);
      return;
    }
    
    try {
      sourceBufferRef.current.appendBuffer(chunk);
    } catch (e) {
      console.error('Error appending to SourceBuffer:', e);
    }
  };
  
  const sendAudioToBackend = () => {
    audioQueueRef.current = [];
    
    if (mediaSourceRef.current) {
      if (mediaSourceRef.current.readyState === 'open') {
        mediaSourceRef.current.endOfStream();
      }
      mediaSourceRef.current = null;
    }
    
    if (sourceBufferRef.current) {
      sourceBufferRef.current = null;
    }
    
    isSourceOpenRef.current = false;
    setupMediaSource();
    
    socketRef.current = new WebSocket('ws://localhost:5000/audio');
    
    socketRef.current.onopen = () => {
      const audioBlob = new Blob(audioChunksRef.current, { type: 'audio/webm' });
      socketRef.current.send(audioBlob);
    };
    
    socketRef.current.onmessage = async (event) => {
      try {
        const arrayBuffer = await event.data.arrayBuffer();
        
        if (isSourceOpenRef.current) {
          appendToSourceBuffer(arrayBuffer);
        } else {
          audioQueueRef.current.push(arrayBuffer);
        }
      } catch (err) {
        console.error('Error processing audio chunk:', err);
      }
    };
    
    socketRef.current.onerror = (error) => {
      console.error('WebSocket error:', error);
      setIsProcessing(false);
    };
    
    socketRef.current.onclose = () => {
      if (mediaSourceRef.current && mediaSourceRef.current.readyState === 'open') {
        setTimeout(() => {
          try {
            mediaSourceRef.current.endOfStream();
          } catch (e) {
            console.error('Error ending stream:', e);
          }
        }, 100);
      }
      
      if (!isTalking) {
        setIsProcessing(false);
      }
    };
  };

  return (
    <div className="app-container">
      <h1>Voice Assistant</h1>
      <p>Powered by OpenAI and .NET 9</p>
      
      <div className="controls">
        <button 
          onClick={isRecording ? stopRecording : startRecording}
          className={isRecording ? "recording" : ""}
          disabled={isProcessing && !isRecording}
        >
          {isRecording ? (
            <>
              <span className="mic-icon">●</span>
              Stop
            </>
          ) : (
            <>
              <span className="mic-icon">🎙️</span>
              Start Recording
            </>
          )}
        </button>
      </div>
      
      <div className="status-indicators">
        {isProcessing && (
          <div className="thinking-indicator">
            <div className="thinking-pulse"></div>
            <p>Processing your request...</p>
          </div>
        )}
        
        {isTalking && (
          <div className="talking-indicator">
            <div className="talking-waves">
              <span></span>
              <span></span>
              <span></span>
              <span></span>
              <span></span>
            </div>
            <p>Assistant is speaking...</p>
          </div>
        )}
      </div>
    </div>
  );
};

export default App;