let uid = "SiteTest1";

let err;

//กำหนดตัวแปรสำหรับเก็บ stream ของ user และ peerConnection
let localStream;

let audiotrack;
//กำหนดตัวแปรสำหรับเก็บ stream ของ user อื่นๆ
let remoteStream = {};
//กำหนดตัวแปรสำหรับเก็บ peerConnection ของ user และ peerConnection ของ user อื่นๆ
let peerConnection = {};

let numberofcandidate = {};

let iceConnectionStateChangeTimers = {};

let audio
//กำหนด server สำหรับเชื่อมด้วย PeerConnection
const servers = {
    iceServers: [
        {
            urls: ['stun:stun1.l.google.com:19302', 'stun:stun2.l.google.com:19302']
        }
    ]
}

//const messageline = document.getElementById('messagesList');
//const messageClear = document.getElementById('clearButton');

let srconnection = null;

// Restore the messages from local storage
const savedsrMessages = localStorage.getItem('messageline');
if (savedsrMessages) {
    messageline.innerHTML = savedsrMessages;
}


function handleDeviceChange(event) {
    console.log('Media device changed:', event);
    // Check if the audio device is still available
    navigator.mediaDevices.getUserMedia({ audio: true })
      .then(stream => {
        localStream = stream;
        audiotrack = localStream.getAudioTracks().find(track => track.kind === 'audio')
        audio = new Audio()
        audio.volume = 0.5
        audio.srcObject = localStream
        document.getElementById('voice-icon').classList.remove('text-danger')
        if ($('#buttonMic').hasClass('active')) { audiotrack.enabled = true } else { audiotrack.enabled = false }
        if ($('#playback-btn').hasClass('active')) { audio.play() } else { audio.pause() }
        updateAudioVisualization(localStream);
      })
      .catch(error => {
        if (peerConnection) close_all();
        err = error;
        localStream = null;
        document.getElementById('voice-icon').classList.add('text-danger')
        Swal.fire({
            icon: 'error',
            title: 'ไม่สามารถเชื่อมต่อกับไมโครโฟนได้',
            text: 'กรุณาตรวจสอบการเชื่อมต่อไมโครโฟนของท่าน และลองเข้าใช้งานใหม่อีกครั้ง ' + error,
            footer: '<a href="https://www.google.com/search?q=chrome+allow+microphone+access&oq=chrome+allow+microphone+access&aqs=chrome..69i57j0l7.10560j0j7&sourceid=chrome&ie=UTF-8" target="_blank">คลิกที่นี่เพื่อดูวิธีการเปิดใช้ไมโครโฟน</a>'
        })
      });
  }
  
  function updateAudioVisualization(stream) {
    const audioContext = new AudioContext();
    const source = audioContext.createMediaStreamSource(stream);
    const analyser = audioContext.createAnalyser();
  
    // Connect the source to the analyser
    source.connect(analyser);
  
    // Start the animation loop
    requestAnimationFrame(update);
    function update() {
      const bufferLength = analyser.frequencyBinCount;
      const dataArray = new Uint8Array(bufferLength);
      analyser.getByteFrequencyData(dataArray);
  
      // Calculate the average volume
      let sum = 0;
      for (let i = 0; i < bufferLength; i++) {
        sum += dataArray[i];
      }
      const average = sum / bufferLength;
  
      // Update the color of the icon based on the volume
      const icon = document.getElementById('voice-icon');
      //document.getElementById('analyzer').innerHTML = average.toFixed(0);
      if (icon.classList.contains('ri-mic-fill')) {
        if (average > 10) {
          icon.classList.add('text-success');
        } else {
          icon.classList.remove('text-success');
        }
      }
      // Call the update function again on the next animation frame
      requestAnimationFrame(update);
    }
  }
  
  // Listen to the devicechange event
  navigator.mediaDevices.addEventListener('devicechange', handleDeviceChange);

// Connect to the SignalR hub
async function srconnect() {
    
    navigator.mediaDevices.getUserMedia({ audio: true })
        .then(stream => {
            localStream = stream
            audiotrack = localStream.getAudioTracks().find(track => track.kind === 'audio')
            audiotrack.enabled = false
            audio = new Audio()
            audio.volume = 0.5
            audio.srcObject = localStream
            updateAudioVisualization(localStream);
        })
        .catch(error => { 
            localStream = null;
            err = error;
            document.getElementById('voice-icon').classList.add('text-danger')
            Swal.fire({
                icon: 'error',
                title: 'ไม่สามารถเชื่อมต่อกับไมโครโฟนได้',
                text: 'กรุณาตรวจสอบการเชื่อมต่อไมโครโฟนของท่าน และลองเข้าใช้งานใหม่อีกครั้ง ' + error,
                footer: '<a href="https://www.google.com/search?q=chrome+allow+microphone+access&oq=chrome+allow+microphone+access&aqs=chrome..69i57j0l7.10560j0j7&sourceid=chrome&ie=UTF-8" target="_blank">คลิกที่นี่เพื่อดูวิธีการเปิดใช้ไมโครโฟน</a>'
            })
        })

    if (!srconnection) {

        srconnection = new signalR.HubConnectionBuilder()
            .withUrl('/chatHub')
            .build();

        srconnection.on('ClientConnected', async function (message) {
            console.log('ClientConnected', message)
            messageobj = JSON.parse(message)
            if (messageobj.id === uid) {
                if (messageobj.type === 'request') {
                    console.log('Requesting offer', messageobj.user)
                    await createPeerConnection(messageobj.user)
                }
                if (messageobj.type === 'answer') {
                    console.log('Adding answer', messageobj.user)
                    addAnswer(messageobj.message, messageobj.user)
                }
                if (messageobj.type === 'candidate') {
                    if (peerConnection[messageobj.user]) {
                        console.log('Adding candidate', messageobj.user)
                        peerConnection[messageobj.user].addIceCandidate(messageobj.message)
                    }
                }

                if (messageobj.type === 'leave') {
                    handleUserLeft(messageobj.user)
                }
            }
        });

        srconnection.start()
            .then(() => {
                console.log('SignalR connection started.');
            })
            .catch(err => {
                console.error('SignalR connection error: ' + err);
                setTimeout(() => connect(), 5000);
            });
    }
}

srconnect();

function getsrCurrentTime() {
    const now = new Date();
    const hours = now.getHours().toString().padStart(2, '0');
    const minutes = now.getMinutes().toString().padStart(2, '0');
    const seconds = now.getSeconds().toString().padStart(2, '0');
    return `${hours}:${minutes}:${seconds}`;
}

async function addCandidate(candidate, MemberID) {
    //เพิ่ม candidate ให้กับ peerConnection ของ user นั้น
    if (peerConnection[MemberID]) {
        await peerConnection[MemberID].addIceCandidate(candidate)
    }
}

async function handleUserLeft(MemberID) {
    //เมื่อมี user ออกจาก channel จะทำการลบ peerConnection และ remoteStream ของ user นั้นออก
    peerConnection[MemberID].close()
    delete peerConnection[MemberID]
    delete remoteStream[MemberID]
    delete numberofcandidate[MemberID]
    delete iceConnectionStateChangeTimers[MemberID]
    document.getElementById('number-of-node').innerHTML = `${onlineUser()} เสา`
    let item = document.getElementById(`user-container-${MemberID}`)
    if (item) {
        item.remove()
    }
}

async function createPeerConnection(MemberID) {
    if (peerConnection[MemberID]) {
        createOffer(MemberID)
        return
    }
    sendMessagetoCore('media', '', MemberID, 'stop')
    sendMessagetoCore('schedule', '', MemberID, 'stop')
    sendMessagetoCore('stream', '', MemberID, 'stop')

    let player = document.getElementById(`user-container-${MemberID}`)
    if (player === null) {
        player = `
                    <div class="col" id="user-container-${MemberID}">
                        <div class="card team-box">
                            <div class="card-body p-2">
                                <div class="row team-row">
                                    <div class="col team-settings">
                                        <div class="card-header align-items-center d-flex" style="height: 30px;">
                                            <div class="card-title mb-0 flex-grow-1">
                                                <h4 class="mb-1 projects-num text-mute" id="status-${MemberID}">${IsOnline.find(obj => obj.id === MemberID) ? "กำลังเชื่อมต่อ" : "ออฟไลน์"}</h4>                                                                                                                 
                                            </div>
                                            <div class="ms-3 flex-shrink-0">
                                                <button type="button" value="${MemberID}" class="btn-close ms-auto" id="customizerclose-btn" data-bs-dismiss="offcanvas" aria-label="Close" onclick="openmodal(this)" data-bs-toggle="modal" data-bs-target="#broardcastModal" modal-type="close"></button>
                                            </div>
                                        </div>
                                    </div>
                                    <div class="col ms-3">
                                        <div class="row align-items-center">
                                            <div class="col my-2">                                                                                                           
                                                <div class="team-content"> 
                                                    <a class="member-name" data-bs-toggle="offcanvas" href="#member-overview" aria-controls="member-overview">
                                                        <h5 class="fs-16 mb-1" id="nodename-${MemberID}">${Model.find(obj => obj.key === MemberID).name}</h5>
                                                    </a>
                                                    <p class="text-muted member-designation mb-0">${Model.find(obj => obj.key === MemberID).description}</p>
                                                </div>                                                    
                                            </div>                                           
                                            <div class="col">
                                                <div class="row align-items-center">
                                                    <div class="col-sm-2">
                                                        <div class="d-flex justify-content-center">
                                                            <button type="button" id="mute-btn-${MemberID}" class="btn btn-md custom-toggle" data-bs-toggle="button" id="mute-button" aria-pressed="false">
                                                                    <span class="icon-on"><i class="ri-volume-up-line ri-xl text-success" style="display: flex; align-items: center;"></i></span>
                                                                    <span class="icon-off"><i class="ri-volume-mute-line ri-xl text-danger" style="display: flex; align-items: center;"></i></span>
                                                            </button>
                                                        </div>
                                                    </div>
                                                    <div class="col-6">
                                                        <input type="range" class="volume" style="width:100%; display: flex; align-items: center;" id="volume-${MemberID}" name="volume-${MemberID}" min="0" max="1000" value="500">
                                                    </div>
                                                </div>
                                            </div>
                                        </div>
                                    </div>
                                </div>
                            </div>
                        </div>
                    </div>
        
`
        document.getElementById('team-member-list').insertAdjacentHTML('beforeend', player)
        document.getElementById(`volume-${MemberID}`).addEventListener('change', function () {
            let volume = document.getElementById(`volume-${MemberID}`).value
            sendMessagetoCore('stream', volume, MemberID, 'ch-vol')
            document.getElementById(`mute-btn-${MemberID}`).classList.remove('active')
        })
        document.getElementById(`mute-btn-${MemberID}`).addEventListener('click', function () {
            if (document.getElementById(`mute-btn-${MemberID}`).classList.contains('active')) {
                sendMessagetoCore('stream', 0, MemberID, 'ch-vol')
            }
            else {
                let volume = document.getElementById(`volume-${MemberID}`).value
                sendMessagetoCore('stream', volume, MemberID, 'ch-vol')
            }
        })
    }

    setTimeout(async () => {

        startIceConnectionStateChangeTimer(MemberID);
        //สร้าง peerConnection ของ user นั้น
        let newpeerConnection = new RTCPeerConnection(servers)
        peerConnection[MemberID] = newpeerConnection
        //เมื่อมี remote stream ใหม่จะทำการเพิ่ม remote stream ให้กับ user นั้น remote stream จะเป็น stream ของ user อื่นที่ต้องการเชื่อมต่อ
        let newremoteStream = new MediaStream()
        remoteStream[MemberID] = newremoteStream

        numberofcandidate[MemberID] = 0;

        //local stream คือ stream ของ user ตัวเอง ถ้ายังไม่มีจะทำการสร้าง stream ของ user ตัวเอง
        if (!localStream) {
            Swal.fire({
                icon: 'error',
                title: 'ไม่สามารถเชื่อมต่อกับไมโครโฟนได้',
                text: 'กรุณาตรวจสอบการเชื่อมต่อไมโครโฟนของท่าน และลองเข้าใช้งานใหม่อีกครั้ง ' + err,
                footer: '<a href="https://www.google.com/search?q=chrome+allow+microphone+access&oq=chrome+allow+microphone+access&aqs=chrome..69i57j0l7.10560j0j7&sourceid=chrome&ie=UTF-8" target="_blank">คลิกที่นี่เพื่อดูวิธีการเปิดใช้ไมโครโฟน</a>'
            })
            handleUserLeft(MemberID)
            return
        }
        //เพิ่ม local stream ให้กับ peerConnection ของ user นั้น
        localStream.getTracks().forEach(track => {
            peerConnection[MemberID].addTrack(track, localStream)
        })

        let offer = await peerConnection[MemberID].createOffer()
        await peerConnection[MemberID].setLocalDescription(offer)

        //เมื่อมี ice candidate ใหม่จะทำการส่ง ice candidate ไปยัง user อื่น
        peerConnection[MemberID].onicecandidate = async (event) => {
            if (event.candidate) {
                numberofcandidate[MemberID]++;
                if (numberofcandidate[MemberID] == 3) {
                    createOffer(MemberID)
                }
                sendMessagetoCore('candidate', event.candidate, MemberID)
            }
        }

        peerConnection[MemberID].oniceconnectionstatechange = async (event) => {
            iceConnectionStateChangeTimers[MemberID].hasEventOccurred = true;
            console.log(MemberID, 'ICE connection state changed to ', peerConnection[MemberID].iceConnectionState)
            var status_text = document.getElementById(`status-${MemberID}`)
            document.getElementById('number-of-node').innerHTML = `${onlineUser()} เสา`
            if (peerConnection[MemberID].iceConnectionState === 'connected') {
                status_text.classList.remove('text-danger')
                status_text.classList.remove('text-muted')
                status_text.classList.add('text-primary')
                status_text.innerHTML = "ถ่ายทอด"
            }
            if (peerConnection[MemberID].iceConnectionState === 'checking') {
                status_text.classList.remove('text-primary')
                status_text.classList.remove('text-danger')
                status_text.classList.add('text-muted')
                status_text.innerHTML = "กำลังเชื่อมต่อ"
            }
            if (peerConnection[MemberID].iceConnectionState === 'disconnected') {
                Swal.fire({
                    title: 'User disconnected',
                    text: `${document.getElementById(`nodename-${MemberID}`).innerHTML} disconnected`,
                    icon: 'warning',
                    confirmButtonText: 'OK'
                })
                handleUserLeft(MemberID)
            }
        }
    }, 2000);

}


function startIceConnectionStateChangeTimer(MemberID) {
    // Set up flag variable and timer for this MemberID
    iceConnectionStateChangeTimers[MemberID] = {
        hasEventOccurred: false,
        timer: setTimeout(() => {
            if (!iceConnectionStateChangeTimers[MemberID].hasEventOccurred && peerConnection[MemberID].iceConnectionState !== 'connected') {
                Swal.fire({
                    title: 'Connection Time out',
                    text: `${Model.find(obj => obj.key === MemberID).name} is Time out`,
                    icon: 'warning',
                    confirmButtonText: 'OK'
                })
                handleUserLeft(MemberID)
            }
        }, 30000)
    };
}

async function createOffer(MemberID) {
    //สร้าง offer ของ user นั้น
    let offer = await peerConnection[MemberID].createOffer()
    await peerConnection[MemberID].setLocalDescription(offer)
    //ส่ง offer ไปยัง user อื่น
    sendMessagetoCore('offer', offer, MemberID)
    // .then(function(successResponse) {
    //     if (successResponse.success) {
    //         Swal.fire({
    //             icon: 'success',
    //             title: 'สำเร็จ..',
    //             text: successResponse.message,
    //             timer: 3000
    //         })
    //     } else {
    //         Swal.fire({
    //             icon: 'error',
    //             title: 'เกิดข้อผิดพลาด...',
    //             text: successResponse.message,
    //             timer: 3000,
    //         })
    //     }
    // })
    // .catch(function(errorResponse) {
    //     Swal.fire({
    //         icon: 'error',
    //         title: 'เกิดข้อผิดพลาด...',
    //         text: errorResponse.responseText,
    //     })
    // });
}

async function addAnswer(answer, MemberID) {
    //เพิ่ม answer ของ user อื่นให้กับ peerConnection ของ user นั้น
    if (!peerConnection[MemberID].currentRemoteDescription) {
        await peerConnection[MemberID].setRemoteDescription(answer)
    }
}

async function sendMessagetoCore(messagetype, message, target_uid, cmd) {

    var data = {
        user: uid,
        type_user: "multi",
        cmd: cmd,
        type: messagetype,
        message: JSON.stringify(message),
        target: target_uid,
        target_multi: [target_uid]
    };
    return new Promise(function (resolve, reject) {
        $.ajax({
            type: "POST",
            url: "/station/WebRTC",
            contentType: "application/json; charset=utf-8",
            data: JSON.stringify(data),
            success: function (response) {
                resolve(response);
            },
            error: function (error) {
                reject(error);
            }
        });
    });
};


async function openbroadcast(allnode) {
    const selectElement = document.getElementById("multiselect-optiongroup");
    const closeModal = document.getElementById('closebroardcastModal');
    var selectedOptions
    if (allnode) {
        selectedOptions = Array.from(selectElement.options).map(option => {
            return {
                value: option.value,
                type: option.getAttribute("data-type"),
                nodeslist: option.getAttribute("data-nodes")
            };
        });
    }
    else {
        selectedOptions = Array.from(selectElement.selectedOptions).map(option => {
            return {
                value: option.value,
                type: option.getAttribute("data-type"),
                nodeslist: option.getAttribute("data-nodes")
            };
        });
    }
    // Validate input
    const errors = [];

    if (selectedOptions.length === 0) {
        errors.push("กรุณาเลือกสถานีกระจายเสียง");
    }
    if (errors.length > 0) {
        const errorHtml = errors.map(error => `<li>${error}</li>`).join("");
        Swal.fire({
            icon: 'error',
            title: 'เกิดข้อผิดพลาด...',
            html: `กรุณาแก้ไขข้อผิดพลาดต่อไปนี้<ul>${errorHtml}</ul>`,
        });
        return; // Stop the function if there are errors
    }

    for (const item of selectedOptions) {
        if (item.type == "group") {
            var nodes = item.nodeslist.split(",");
            for (const node of nodes) {
                //เรียกใช้ function createPeerConnection สำหรับสร้าง peerConnection ของ user นั้น
                await createPeerConnection(node)
            }
        } else {
            //เรียกใช้ function createPeerConnection สำหรับสร้าง peerConnection ของ user นั้น
            await createPeerConnection(item.value)
        }
    }
    closeModal.click();
};

function kick(MemberID) {
    handleUserLeft(MemberID)
    sendMessagetoCore('stream', '', MemberID, 'stop')
}

function close_all() {
    for (const item of Object.keys(peerConnection)) {
        handleUserLeft(item)
    }   
    sendMessagetoCore('stream', '', 'all', 'stop')
    Swal.fire({
        icon: 'success',
        title: 'สำเร็จ..',
        text: 'ปิดการกระจายเสียงสำเร็จ',
        timer: 3000
    })
};

function mute_all(mutestate) {
    if (mutestate) {
        for (const item of Object.keys(peerConnection)) {
            sendMessagetoCore('stream', 0, item, 'ch-vol')
            $(`#mute-btn-${item}`).addClass("active");
        }
        Swal.fire({
            icon: 'success',
            title: 'สำเร็จ..',
            text: 'ปิดเสียงสำเร็จ',
            timer: 3000
        })
    } else {
        for (const item of Object.keys(peerConnection)) {
            sendMessagetoCore('stream', $(`#volume-${item}`).val(), item, 'ch-vol')
            $(`#mute-btn-${item}`).removeClass("active");
        }
        Swal.fire({
            icon: 'success',
            title: 'สำเร็จ..',
            text: 'เปิดเสียงสำเร็จ',
            timer: 3000
        })
    }
}

async function toggleMic() {
    let audiotrack = localStream.getAudioTracks().find(track => track.kind === 'audio')
    let buttonMic = document.getElementById('buttonMic')
    let icon = document.getElementById('voice-icon')
    if (audiotrack.enabled) {
        audiotrack.enabled = false
        icon.classList.remove('text-success')
        icon.classList.remove('ri-mic-fill')
        icon.classList.add('ri-mic-off-fill')
        buttonMic.classList.remove('btn-success')
    } else {
        audiotrack.enabled = true
        icon.classList.remove('ri-mic-off-fill')
        icon.classList.add('ri-mic-fill')
        buttonMic.classList.add('btn-success')
    }
}

function onlineUser() {
    let connectedPeerCount = 0;
    for (const pc of Object.values(peerConnection)) {
        if (pc.iceConnectionState === 'connected') {
            connectedPeerCount++;
        }
    }
    return connectedPeerCount;
}

document.getElementById('buttonMic').addEventListener('click', toggleMic)

document.getElementById('playback-btn').addEventListener('click', function () {
    if (audio.paused) {
        audio.play()
    }
    else {
        audio.pause()
    }
})


