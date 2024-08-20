//กำหนดตัวแปรสำหรับเก็บ stream ของ user และ peerConnection
let localStream;
//กำหนดตัวแปรสำหรับเก็บ stream ของ user อื่นๆ
let remoteStream = {};
//กำหนดตัวแปรสำหรับเก็บ peerConnection ของ user และ peerConnection ของ user อื่นๆ
let peerConnection = {};

//กำหนด server สำหรับเชื่อมด้วย PeerConnection
const servers = {
    iceServers: [
        {
            urls: ['stun:stun1.l.google.com:19302', 'stun:stun2.l.google.com:19302']
        }
    ]
}

//กำหนด function สำหรับเริ่มต้นการทำงาน
let init = async () => {

    //กำหนด Track ของ user ให้กับ localStream
    localStream = await navigator.mediaDevices.getUserMedia({ video: false, audio: true })

}

let handleUserLeft = async (MemberID) => {
    //เมื่อมี user ออกจาก channel จะทำการลบ peerConnection และ remoteStream ของ user นั้นออก
    delete peerConnection[MemberID]
    delete remoteStream[MemberID]

    //ลบ element ของ user นั้นออกจากหน้าจอ
    let item = document.getElementById(`user-container-${MemberID}`)
    if (item) {
        item.remove()
    }
}

//เมื่อมี user ส่งข้อความแบบระบุด้วย ID
let handleMessageFromPeer = async (message, MemberID) => {
    message = JSON.parse(message.text)

    //ถ้ามี user ส่งข้อความแบบระบุด้วย ID และมีข้อความเป็น request จะทำการสร้าง offer และส่งข้อความกลับไปยัง user นั้น
    if (message.type === 'request') {
        console.log('Requesting offer', MemberID)
        createOffer(MemberID)
    }
    //offer คือ ข้อเสนอของ user ที่ต้องการเชื่อมต่อกับ user อื่น

    //ถ้ามี user ส่งข้อความแบบระบุด้วย ID และมีข้อความเป็น answer จะทำการเพิ่ม answer ให้กับ peerConnection ของ user นั้น
    if (message.type === 'answer') {
        console.log('Adding answer', MemberID)
        addAnswer(message.answer, MemberID)
    }
    //answer คือ ข้อเสนอของ user ที่ตอบกลับเมื่อ user อื่นส่งข้อเสนอมา

    //ถ้ามี user ส่งข้อความแบบระบุด้วย ID และมีข้อความเป็น candidate จะทำการเพิ่ม candidate ให้กับ peerConnection ของ user นั้น
    if (message.type === 'candidate') {
        if (peerConnection[MemberID]) {
            console.log('Adding candidate', MemberID)
            peerConnection[MemberID].addIceCandidate(message.candidate)
        }
    }
    //candidate คือ ข้อมูลที่ใช้ในการสร้าง connection ระหว่าง peer ทั้งสอง

}

let handleUserJoined = async (MemberID) => {
    console.log('A new user joined: ', MemberID)
    createOffer(MemberID)
}

let createPeerConnection = async (MemberID) => {
    //ถ้ามี peerConnection ของ user นั้นอยู่แล้วจะไม่ทำการสร้างใหม่
    if (peerConnection[MemberID]) {
        return
    }

    //สร้าง peerConnection ของ user นั้น
    let newpeerConnection = new RTCPeerConnection(servers)
    peerConnection[MemberID] = newpeerConnection
    //เมื่อมี remote stream ใหม่จะทำการเพิ่ม remote stream ให้กับ user นั้น remote stream จะเป็น stream ของ user อื่นที่ต้องการเชื่อมต่อ
    let newremoteStream = new MediaStream()
    remoteStream[MemberID] = newremoteStream

    //สร้าง element ของ user นั้น
    let player = document.getElementById(`nodes-container-${MemberID}`)
    if (player === null) {
        player = `<div class="video__container" id="user-container-${MemberID}">
                    
                    <div>
                        <h3 style="font-family: Arial, sans-serif; margin:10px;" class="video__label" id="video__label-${MemberID}">${MemberID}</h3>
                    </div>
                    <div>
                        <label for="volume-${MemberID}">Volume:</label>
                        <input type="range" class="volume" id="volume-${MemberID}" name="volume-${MemberID}" min="0" max="1000" value="500">
                     </div>               
                </div> `
        document.getElementById('nodes_container').insertAdjacentHTML('beforeend', player)

        //เมื่อมีการเปลี่ยนแปลง volume จะทำการส่งข้อความไปยัง user นั้น ให้เปลี่ยน volume ของ stream ของตัวเอง
        document.getElementById(`volume-${MemberID}`).addEventListener('change', function () {
            let volume = document.getElementById(`volume-${MemberID}`).value
            client.sendMessageToPeer({ text: JSON.stringify({ 'type': 'volume_change', 'uid': uid, 'volume': volume, 'target_uid': MemberID }) }, MemberID)
            sendMqtt(JSON.stringify({ 'type': 'volume_change', 'uid': uid, 'volume': volume, 'target_uid': MemberID }))
        })
    }

    //local stream คือ stream ของ user ตัวเอง ถ้ายังไม่มีจะทำการสร้าง stream ของ user ตัวเอง
    if (!localStream) {
        localStream = await navigator.mediaDevices.getUserMedia({ video: false, audio: true })
    }

    //เพิ่ม local stream ให้กับ peerConnection ของ user นั้น
    localStream.getTracks().forEach(track => {
        peerConnection[MemberID].addTrack(track, localStream)
    })

    //เมื่อมี ice candidate ใหม่จะทำการส่ง ice candidate ไปยัง user อื่น
    peerConnection[MemberID].onicecandidate = async (event) => {
        if (event.candidate) {
            client.sendMessageToPeer({ text: JSON.stringify({ 'type': 'candidate', 'candidate': event.candidate }) }, MemberID)
            sendMqtt(JSON.stringify({ 'type': 'candidate', 'candidate': event.candidate }))
        }
    }

    peerConnection[MemberID].oniceconnectionstatechange = async (event) => {
        console.log(MemberID, 'ICE connection state changed to ', peerConnection[MemberID].iceConnectionState)
        if (peerConnection[MemberID].iceConnectionState === 'disconnected') {
            handleUserLeft(MemberID)
        }
    }
}

let createOffer = async (MemberID) => {
    //เรียกใช้ function createPeerConnection สำหรับสร้าง peerConnection ของ user นั้น
    await createPeerConnection(MemberID)
    //สร้าง offer ของ user นั้น
    let offer = await peerConnection[MemberID].createOffer()
    await peerConnection[MemberID].setLocalDescription(offer)
    //ส่ง offer ไปยัง user อื่น
    client.sendMessageToPeer({ text: JSON.stringify({ 'type': 'offer', 'offer': offer }) }, MemberID)
    sendMqtt(JSON.stringify({ 'type': 'offer', 'offer': offer }))

}

let addAnswer = async (answer, MemberID) => {
    //เพิ่ม answer ของ user อื่นให้กับ peerConnection ของ user นั้น
    if (!peerConnection[MemberID].currentRemoteDescription) {
        await peerConnection[MemberID].setRemoteDescription(answer)
    }
}

let leaveChannel = async () => {
    await channel.leave()
    await client.logout()
}

let toggleMic = async () => {
    let audiotrack = localStream.getAudioTracks().find(track => track.kind === 'audio')

    if (audiotrack.enabled) {
        audiotrack.enabled = false
        document.getElementById('mic-btn').style.backgroundColor = 'rgb(255,80,80, 1)'
    } else {
        audiotrack.enabled = true
        document.getElementById('mic-btn').style.backgroundColor = 'rgba(159, 215, 61, 0.9)'
    }
}

window.addEventListener('beforeunload', leaveChannel)
document.getElementById('mic-btn').addEventListener('click', toggleMic)
document.getElementById('check-btn').addEventListener('click', () => {
    Object.keys(peerConnection).forEach(function (key) {
        peerConnection[key].getStats().then(stats => {
            console.log(stats)
        })
    });
})

init()