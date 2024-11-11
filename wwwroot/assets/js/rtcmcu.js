const client = AgoraRTC.createClient({ mode: "rtc", codec: "h264" });
AgoraRTC.setLogLevel(3);

        let peerConnections = {};
        let remoteStreamsTrack = null;
        let numberofcandidate = {};
        let iceConnectionStateChangeTimers = {};
        let currentUID = null;

        var servers = { 'iceServers': [{ 'urls': 'stun:stun.l.google.com:19302' }] };

        client.on("user-published", handleUserPublished);
        client.on("user-unpublished", handleUserUnpublished);
        client.on("user-left", handleUserUnpublished);
        client.on("connection-state-change", (curState, prevState, reason) => {
            console.log("connection-state-change", curState, prevState, reason);
            if (curState === "DISCONNECTED") {
                Object.keys(peerConnections).forEach(async (key) => {
                    peerConnections[key].close();
                    delete numberofcandidate[key];
                    delete iceConnectionStateChangeTimers[key];
                    delete peerConnections[key];
                    await sendMessagetoCloud("disconnected", null, currentUID, `rtcuser`, key);
                });
            }
        });

            // Connect to the SignalR hub
    function connect() {
        if (!srconnection) {
            srconnection = new signalR.HubConnectionBuilder()
                .withUrl('/chatHub')
                .build();

            srconnection.on('ClientConnected', async function (message) {
                console.log("message-core", message);
                const messageobj = JSON.parse(message);
                if (messageobj.type == "offer") {
                await peerConnections[messageobj.user].setRemoteDescription(messageobj.message);
                const answer = await peerConnections[messageobj.id].createAnswer();
                await peerConnections[messageobj.user].setLocalDescription(answer);
                sendMessagetoCore("answer", answer, messageobj.user, `/sub/node`, messageobj.id);
            }
                if (messageobj.type == "answer") {
                    console.log("answer", messageobj);
                    await peerConnections[messageobj.user].setRemoteDescription(messageobj.message);
                }
                if (messageobj.type == "candidate") {
                    await peerConnections[messageobj.user].addIceCandidate(messageobj.message);
                }
            });

            srconnection.on('Signaling', async function (message) {
                console.log("message-cloud", message);
                const messageobj = JSON.parse(message);
                if (messageobj.type == "request") {
                    if (client.connectionState == "DISCONNECTED") {
                        console.log("client join", options);
                        client.join(options.appid, options.channel, options.token, options.uid)
                    }
                    console.log(messageobj.id[0]);
                    for (let i = 0; i < messageobj.id.length; i++) {
                        console.log('%c' + messageobj.id[i], 'color: red; font-size: 20px;');

                        await createPeerConnection(messageobj.user,messageobj.id[i],messageobj.vol[i]);
                    }
                }
                if (messageobj.type == "ch-vol") {
                    console.log("ch-vol", messageobj);
                    for (let i = 0; i < messageobj.id.length; i++) {
                        let chvolobj = {
                            type: "stream",
                            message: JSON.stringify(`${messageobj.message[i]}`),
                            user: messageobj.user,
                            id: messageobj.id[i],
                            cmd: 'ch-vol'
                        }
                        srconnection.invoke('PublishLocal', JSON.stringify(chvolobj), `/sub/node`);
                    }
                }
                if (messageobj.type == "kick") {
                    for (let i = 0; i < messageobj.id.length; i++) {
                        if (peerConnections[messageobj.id[i]])
                        {
                            sendMessagetoCore("stream", null, messageobj.user, `/sub/node`, messageobj.id[i], "stop");
                            peerConnections[messageobj.id[i]].close();
                            delete peerConnections[messageobj.id[i]];
                            delete numberofcandidate[messageobj.id[i]];
                            delete iceConnectionStateChangeTimers[messageobj.id[i]];          
                        }
                    }
                    if (Object.keys(peerConnections).length == 0) {
                        client.leave();
                    }
                }
            })

            srconnection.start()
                .then(() => {
                    console.log('SignalR connection started.');
                })
                .catch(err => {
                    console.error('SignalR connection error: ' + err);
                    // Try to reconnect in 5 seconds
                    setTimeout(() => connect(), 5000);
                });
        }
    }

    // Try to connect on page load
    connect();
        

        async function sendMessagetoCore(type, message, user, topic, key, cmd = null) {
            let messageobj = {
                type: type,
                message: JSON.stringify(message),
                user: user,
                id: key,
                cmd: cmd
            }
            srconnection.invoke('PublishLocal', JSON.stringify(messageobj), topic);
        }

        async function sendMessagetoCloud(type, message, user, topic, key, cmd = null) {
            let messageobj = {
                type: type,
                message: JSON.stringify(message),
                user: user,
                id: key,
                cmd: cmd
            }
            srconnection.invoke('PublishEMQX', JSON.stringify(messageobj), topic);
        }


        async function createPeerConnection(user,key,vol = 50) {
            try {
                console.log("createPeerConnection", key);
                if (peerConnections[key]) {
                    console.log("peerConnections[key] already exist");
                    sendMessagetoCloud("connected", "connected", user, `rtcuser`, key);
                    return;
                }

                console.log('%cCreate Peer Connection #' + key, 'color: red; font-size: 20px;');

                sendMessagetoCore("media", null, user, `/sub/node`, key, "stop")
                sendMessagetoCore("schedule", null, user, `/sub/node`, key, "stop")
                sendMessagetoCore("stream", null, user, `/sub/node`, key, "stop")
                currentUID = user;
                setTimeout(async () => {
                    
                    peerConnections[key] = new RTCPeerConnection(servers);
                    //Check peerConnections[key] is Created
                    if (!peerConnections[key]) {
                        console.log("%cpeerConnections[key] is not Created", 'color: red; font-size: 50px;');
                        return;
                    }

                    console.log("%cpeerConnections[key] is Created", 'color: red; font-size: 50px;');

                    startNodeIceConnectionStateChangeTimer(user, key);
                    numberofcandidate[key] = 0;

                    peerConnections[key].addTrack(remoteStreamsTrack);

                    const offer = await peerConnections[key].createOffer();
                    await peerConnections[key].setLocalDescription(offer);


                    peerConnections[key].onicecandidate = function (event) {
                        if (event.candidate) {
                            numberofcandidate[key]++;
                            console.log("onicecandidate", numberofcandidate[key]);
                            if (numberofcandidate[key] == 2) {
                                createOffer(user,key,vol);
                            }
                            sendMessagetoCore("candidate", event.candidate, user, '/sub/node', key);
                        }
                    };

                    peerConnections[key].oniceconnectionstatechange = function (event) {
                        console.log("oniceconnectionstatechange", peerConnections[key].iceConnectionState);
                        iceConnectionStateChangeTimers[key].hasEventOccurred = true;
                        if (peerConnections[key].iceConnectionState == "connected") {
                            console.log('%cConnected' + key, 'color: red; font-size: 20px;');
                            sendMessagetoCloud("connected", "connected", user, `rtcuser`, key);
                        }
                        if (peerConnections[key].iceConnectionState == "disconnected") {
                            console.log('%cDisconnected' + key, 'color: red; font-size: 20px;');
                            sendMessagetoCloud("disconnected", "disconnected", user, `rtcuser`, key);
                            delete numberofcandidate[key];
                            delete iceConnectionStateChangeTimers[key];       
                            delete peerConnections[key];                   
                        }
                    };

                }, 2000);

            } catch (error) {
                console.error("createPeerConnection", error);
            }
        }

        async function createOffer(user,key,vol) {
            const offer = await peerConnections[key].createOffer()
            await peerConnections[key].setLocalDescription(offer)
            let messageobj = {
                type: "offer",
                message: JSON.stringify(offer),
                user: user,
                id: key,
                vol: vol
            }
            srconnection.invoke('PublishLocal',JSON.stringify(messageobj), `/sub/node`);
        }

        async function handleUserPublished(user, mediaType) {
            console.log("handleUserPublished", user, mediaType);
            await client.subscribe(user, mediaType);
            remoteStreamsTrack = await user.audioTrack.getMediaStreamTrack();
            Object.keys(peerConnections).forEach(async (key) => {
                peerConnections[key].addTrack(remoteStreamsTrack);
            });
        }

        async function handleUserUnpublished(user) {
            console.log("handleUserUnpublished", user);
            if (Object.keys(peerConnections).length != 0) {
                Object.keys(peerConnections).forEach(async (key) => {
                    if (peerConnections[key]) {
                        await sendMessagetoCore("stream", null, user, `/sub/node`, key, "stop");
                        peerConnections[key].close();
                        delete peerConnections[key];
                        delete numberofcandidate[key];
                        delete iceConnectionStateChangeTimers[key];                        
                    }
                });
            }
            client.leave();
        }

        async function startNodeIceConnectionStateChangeTimer(user, key) {
            iceConnectionStateChangeTimers[key] = {
                hasEventOccurred: false,
                timer: setTimeout(() => {
                    if (!iceConnectionStateChangeTimers[key].hasEventOccurred) {
                        console.log("iceConnectionStateChangeTimer", key);
                        console.log('%cTimeout' + key, 'color: red; font-size: 20px;');
                        console.log(peerConnections[key].iceConnectionState);
                        delete peerConnections[key];
                        delete numberofcandidate[key];
                        delete iceConnectionStateChangeTimers[key];
                        sendMessagetoCore("disconnected", null, user, `rtcuser`, key);
                    }
                }, 10000)
            }
        }
    

