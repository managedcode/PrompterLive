// SPDX-License-Identifier: MIT
/**
 * WebRTC Adapter for VDO.Ninja SDK
 * 
 * This adapter provides a unified interface for different WebRTC implementations
 * in Node.js environments. It supports:
 * - Browser native WebRTC (default)
 * - @roamhq/wrtc (Node.js)
 * - node-datachannel (Node.js)
 * 
 * The adapter automatically detects available implementations and provides
 * a consistent API regardless of which library is being used.
 */

class WebRTCAdapter {
    constructor() {
        this.implementation = null;
        this.RTCPeerConnection = null;
        this.RTCSessionDescription = null;
        this.RTCIceCandidate = null;
        this.mediaDevices = null;
        this.MediaStream = null;
        this.MediaStreamTrack = null;
        this.nonstandard = {};
        
        this._detectImplementation();
    }
    
    /**
     * Detect and load available WebRTC implementation
     * @private
     */
    _detectImplementation() {
        // 1. Check if we're in a browser environment
        if (typeof window !== 'undefined' && window.RTCPeerConnection) {
            this.implementation = 'browser';
            this.RTCPeerConnection = window.RTCPeerConnection;
            this.RTCSessionDescription = window.RTCSessionDescription;
            this.RTCIceCandidate = window.RTCIceCandidate;
            this.mediaDevices = navigator.mediaDevices;
            return;
        }
        
        // 2. Try @roamhq/wrtc first (most compatible)
        try {
            const wrtc = require('@roamhq/wrtc');
            this.implementation = '@roamhq/wrtc';
            this.RTCPeerConnection = wrtc.RTCPeerConnection;
            this.RTCSessionDescription = wrtc.RTCSessionDescription;
            this.RTCIceCandidate = wrtc.RTCIceCandidate;
            this.mediaDevices = wrtc.mediaDevices || this._createMediaDevicesShim(wrtc);
            this.MediaStream = wrtc.MediaStream;
            this.MediaStreamTrack = wrtc.MediaStreamTrack;
            this.nonstandard = wrtc.nonstandard || {};
            return;
        } catch (e) {
            // @roamhq/wrtc not available
        }
        
        // 3. Try legacy wrtc
        try {
            const wrtc = require('wrtc');
            this.implementation = 'wrtc';
            this.RTCPeerConnection = wrtc.RTCPeerConnection;
            this.RTCSessionDescription = wrtc.RTCSessionDescription;
            this.RTCIceCandidate = wrtc.RTCIceCandidate;
            this.mediaDevices = wrtc.mediaDevices || this._createMediaDevicesShim(wrtc);
            this.MediaStream = wrtc.MediaStream;
            this.MediaStreamTrack = wrtc.MediaStreamTrack;
            this.nonstandard = wrtc.nonstandard || {};
            return;
        } catch (e) {
            // wrtc not available
        }
        
        // 4. Try node-datachannel (requires wrapper)
        try {
            const nodeDataChannel = require('node-datachannel');
            this.implementation = 'node-datachannel';
            this._setupNodeDataChannel(nodeDataChannel);
            return;
        } catch (e) {
            // node-datachannel not available
        }
        
        // 5. No WebRTC implementation found
        throw new Error(
            'No WebRTC implementation found. Please install one of:\n' +
            '- @roamhq/wrtc (recommended)\n' +
            '- node-datachannel\n' +
            'Or use this SDK in a browser environment.'
        );
    }
    
    /**
     * Create a media devices shim for wrtc implementations
     * @private
     */
    _createMediaDevicesShim(wrtc) {
        return {
            getUserMedia: async (constraints) => {
                // Basic implementation - can be extended
                if (wrtc.getUserMedia) {
                    return new Promise((resolve, reject) => {
                        wrtc.getUserMedia(constraints, resolve, reject);
                    });
                }
                throw new Error('getUserMedia not supported in this wrtc implementation');
            },
            enumerateDevices: async () => {
                // Return empty array if not supported
                return [];
            }
        };
    }
    
    /**
     * Setup node-datachannel with WebRTC-compatible wrapper
     * @private
     */
    _setupNodeDataChannel(nodeDataChannel) {
        const self = this;
        
        // Create RTCPeerConnection wrapper
        this.RTCPeerConnection = class RTCPeerConnectionWrapper {
            constructor(config) {
                this._pc = new nodeDataChannel.PeerConnection(
                    config?.iceServers?.[0]?.urls?.[0] || '',
                    {
                        iceServers: config?.iceServers || [],
                        iceTransportPolicy: config?.iceTransportPolicy || 'all'
                    }
                );
                
                this.localDescription = null;
                this.remoteDescription = null;
                this.signalingState = 'stable';
                this.iceConnectionState = 'new';
                this.iceGatheringState = 'new';
                this.connectionState = 'new';
                
                this._dataChannels = new Map();
                this._tracks = [];
                this._streams = [];
                
                // Event handlers
                this.onicecandidate = null;
                this.oniceconnectionstatechange = null;
                this.onicegatheringstatechange = null;
                this.onsignalingstatechange = null;
                this.ondatachannel = null;
                this.ontrack = null;
                
                // Setup internal event handlers
                this._setupEventHandlers();
            }
            
            _setupEventHandlers() {
                this._pc.onLocalDescription((sdp, type) => {
                    this.localDescription = { sdp, type };
                    this.signalingState = 'have-local-offer';
                });
                
                this._pc.onLocalCandidate((candidate, mid) => {
                    if (this.onicecandidate) {
                        this.onicecandidate({
                            candidate: candidate ? {
                                candidate: candidate,
                                sdpMid: mid,
                                sdpMLineIndex: 0
                            } : null
                        });
                    }
                });
                
                this._pc.onStateChange((state) => {
                    this.iceConnectionState = state;
                    if (this.oniceconnectionstatechange) {
                        this.oniceconnectionstatechange();
                    }
                });
                
                this._pc.onDataChannel((dc) => {
                    const channel = this._wrapDataChannel(dc);
                    if (this.ondatachannel) {
                        this.ondatachannel({ channel });
                    }
                });
            }
            
            async createOffer(options) {
                const sdp = await this._pc.createOffer();
                return { sdp, type: 'offer' };
            }
            
            async createAnswer(options) {
                const sdp = await this._pc.createAnswer();
                return { sdp, type: 'answer' };
            }
            
            async setLocalDescription(desc) {
                this._pc.setLocalDescription(desc.type);
                this.localDescription = desc;
                this.signalingState = desc.type === 'offer' ? 'have-local-offer' : 'stable';
            }
            
            async setRemoteDescription(desc) {
                this._pc.setRemoteDescription(desc.sdp, desc.type);
                this.remoteDescription = desc;
                this.signalingState = desc.type === 'offer' ? 'have-remote-offer' : 'stable';
            }
            
            async addIceCandidate(candidate) {
                if (candidate && candidate.candidate) {
                    this._pc.addRemoteCandidate(candidate.candidate, candidate.sdpMid || '0');
                }
            }
            
            createDataChannel(label, options) {
                const dc = this._pc.createDataChannel(label, options);
                return this._wrapDataChannel(dc);
            }
            
            _wrapDataChannel(dc) {
                const wrapper = {
                    label: dc.label,
                    id: dc.id,
                    readyState: 'connecting',
                    bufferedAmount: 0,
                    bufferedAmountLowThreshold: 0,
                    binaryType: 'arraybuffer',
                    
                    // Event handlers
                    onopen: null,
                    onclose: null,
                    onmessage: null,
                    onerror: null,
                    
                    send: (data) => {
                        if (typeof data === 'string') {
                            dc.sendMessage(data);
                        } else {
                            dc.sendMessageBinary(new Uint8Array(data));
                        }
                    },
                    
                    close: () => {
                        dc.close();
                    }
                };
                
                dc.onOpen(() => {
                    wrapper.readyState = 'open';
                    if (wrapper.onopen) wrapper.onopen();
                });
                
                dc.onClosed(() => {
                    wrapper.readyState = 'closed';
                    if (wrapper.onclose) wrapper.onclose();
                });
                
                dc.onMessage((msg) => {
                    if (wrapper.onmessage) {
                        wrapper.onmessage({ data: msg });
                    }
                });
                
                dc.onError((error) => {
                    if (wrapper.onerror) wrapper.onerror(error);
                });
                
                return wrapper;
            }
            
            // Media-related methods (limited support in node-datachannel)
            addTrack(track, stream) {
                console.warn('Media tracks not fully supported in node-datachannel. Use data channels for media transport.');
                this._tracks.push({ track, stream });
                return { track, streams: [stream] };
            }
            
            removeTrack(sender) {
                const index = this._tracks.findIndex(t => t.track === sender.track);
                if (index !== -1) {
                    this._tracks.splice(index, 1);
                }
            }
            
            getSenders() {
                return this._tracks.map(t => ({ track: t.track }));
            }
            
            getReceivers() {
                return [];
            }
            
            getTransceivers() {
                return [];
            }
            
            close() {
                this._pc.close();
                this.signalingState = 'closed';
                this.iceConnectionState = 'closed';
                this.connectionState = 'closed';
            }
        };
        
        // RTCSessionDescription wrapper
        this.RTCSessionDescription = class {
            constructor(init) {
                this.type = init.type;
                this.sdp = init.sdp;
            }
        };
        
        // RTCIceCandidate wrapper
        this.RTCIceCandidate = class {
            constructor(init) {
                this.candidate = init.candidate;
                this.sdpMid = init.sdpMid;
                this.sdpMLineIndex = init.sdpMLineIndex;
            }
        };
        
        // Media devices stub (no camera/mic support in node-datachannel)
        this.mediaDevices = {
            getUserMedia: async (constraints) => {
                throw new Error('getUserMedia not supported with node-datachannel. Use @roamhq/wrtc for media capture.');
            },
            enumerateDevices: async () => []
        };
    }
    
    /**
     * Get the detected implementation name
     */
    getImplementation() {
        return this.implementation;
    }
    
    /**
     * Check if media (audio/video) is supported
     */
    hasMediaSupport() {
        return this.implementation !== 'node-datachannel';
    }
    
    /**
     * Create a new RTCPeerConnection with the detected implementation
     */
    createPeerConnection(config) {
        if (!this.RTCPeerConnection) {
            throw new Error('No WebRTC implementation available');
        }
        return new this.RTCPeerConnection(config);
    }
    
    /**
     * Get user media (camera/microphone)
     */
    async getUserMedia(constraints) {
        if (!this.mediaDevices || !this.mediaDevices.getUserMedia) {
            throw new Error('getUserMedia not supported in current implementation');
        }
        return this.mediaDevices.getUserMedia(constraints);
    }
}

// Export for different module systems
if (typeof module !== 'undefined' && module.exports) {
    module.exports = WebRTCAdapter;
} else if (typeof define === 'function' && define.amd) {
    define([], () => WebRTCAdapter);
} else {
    window.WebRTCAdapter = WebRTCAdapter;
}
