let uid = "SiteTest1"

let localTracks;
let remoteStream;
let peerConnection;
let remoteUsers = {}

let init = async () => {


    client = AgoraRTC.createClient({ mode: 'rtc', codec: 'vp8' })
    await client.join(APP_ID, roomID, token, uid)

    localTracks = await AgoraRTC.createMicrophoneAudioTrack()
    client.on('user-published', handleUserPublished)
    client.on('user-left', handleUserLeft)
    client.on('user-joined', handleUserJoined)
    client.publish([localTracks])
    localTracks.setMuted(true)
}

let handleUserJoined = async (user) => {
    remoteUsers[user.uid] = user
    channel.sendMessage({ text: JSON.stringify({ 'type': 'user_joined', 'uid': uid }) })
    let player = document.getElementById(`user-container-${user.uid}`)
    if (player === null) {
        player = `
                            <div class="col-xl-3 col-md-6" id="user-container-${user.uid}">
                            <div class="card card-animate">
                                <div class="card-body">                                         
                                    <div class="d-flex align-items-end justify-content-between mt-4 ">
                                                <p class="text-uppercase fw-medium text-muted text-truncate mb-0">
                                                  ${document.getElementById(`option-${user.uid}`).innerHTML}
                                                </p>
                                            <button type="button" class="btn btn-danger btn-sm" id="kick-btn-${user.uid}">
                                                    ✖
                                            </button>
                                    </div>
                                    <div class="d-flex align-items-end justify-content-between mt-4 ">
                                        <button type="button" class="btn btn-secondary btn-sm custom-toggle mt-3" data-bs-toggle="button" id="mute-btn-${user.uid}">
                                            <span class="icon-on"><i class="ri-volume-up-line align-bottom me-1"></i>Mute</span>
                                            <span class="icon-off"><i class="ri-volume-mute-line align-bottom me-1"></i>Unmute</span>
                                        </button>
                                        <input type="range" class="volume" id="volume-${user.uid}" name="volume-${user.uid}" min="0" max="1000" value="500">                
                                       </div>
                                </div><!--end card body-- >
                            </div >< !--end card-- >
                        </div >< !--end col-- >

        `
        document.getElementById('row-1').insertAdjacentHTML('beforeend', player)
        document.getElementById(`volume-${user.uid}`).addEventListener('change', function () {
            let volume = document.getElementById(`volume-${user.uid}`).value
            channel.sendMessage({ text: JSON.stringify({ 'type': 'volume_change', 'uid': uid, 'volume': volume, 'target_uid': user.uid }) })
        })
        document.getElementById(`mute-btn-${user.uid}`).addEventListener('click', function () {
            if (document.getElementById(`mute-btn-${user.uid}`).classList.contains('active')) {
                channel.sendMessage({ text: JSON.stringify({ 'type': 'volume_change', 'uid': uid, 'volume': 0, 'target_uid': user.uid }) })
            }
            else {
                let volume = document.getElementById(`volume-${user.uid}`).value
                channel.sendMessage({ text: JSON.stringify({ 'type': 'volume_change', 'uid': uid, 'volume': volume, 'target_uid': user.uid }) })              
            }
        })

        document.getElementById(`kick-btn-${user.uid}`).addEventListener('click', function () {
            var broadcast = false;
            var selectedOptions = [user.uid]

            // Validate input
            const errors = [];

            if (errors.length > 0) {
                const errorHtml = errors.map(error => `<li>${error}</li>`).join("");
                Swal.fire({
                    icon: 'error',
                    title: 'เกิดข้อผิดพลาด...',
                    html: `กรุณาแก้ไขข้อผิดพลาดต่อไปนี้<ul>${errorHtml}</ul>`,
                });

                return; // Stop the function if there are errors
            }

            var data = {
                key: "1234567890",
                key_multi: selectedOptions,
                type_user: "multi",
                broadcast: broadcast,
            };

            alert(JSON.stringify(data)) 
            $.ajax({
                type: "POST",
                url: "/station/broadcast",
                contentType: "application/json; charset=utf-8",
                data: JSON.stringify(data),
                success: function (response) {
                    if (response.success) {
                        Swal.fire({
                            icon: 'success',
                            title: 'สำเร็จ..',
                            text: response.message,
                            timer: 3000
                        }).then(() => {
                            broadcast = !broadcast;
                        })
                    } else {
                        Swal.fire({
                            icon: 'error',
                            title: 'เกิดข้อผิดพลาด...',
                            text: response.message,
                            timer: 3000,
                        })
                    }
                },
                error: function (error) {
                    Swal.fire({
                        icon: 'error',
                        title: 'เกิดข้อผิดพลาด...',
                        text: error.responseText,
                    })
                }
            });
        })
    }
    document.getElementById('number-of-node').innerHTML = `สถานีที่กำลังรับถ่ายทอด : ${Object.keys(remoteUsers).length}`
}

let handleUserPublished = async (user, mediaType) => {
    await client.subscribe(user, mediaType)
    user.audioTrack.play()
}

let handleUserLeft = async (user) => {
    delete remoteUsers[user.uid]
    let item = document.getElementById(`user-container-${user.uid}`)
    if (item) {
        item.remove()
    }
    document.getElementById('number-of-node').innerHTML = `สถานีที่กำลังรับถ่ายทอด : ${Object.keys(remoteUsers).length}`
}

let toggleMic = async () => {
    let icon = document.getElementById('voice-icon')
    if (localTracks.muted) {
        icon.classList.remove('ri-mic-off-fill')
        icon.classList.add('ri-mic-fill')
        await localTracks.setMuted(false)
    } else {
        icon.classList.remove('text-success')
        icon.classList.remove('ri-mic-fill')
        icon.classList.add('ri-mic-off-fill')
        await localTracks.setMuted(true)
    }
}


let leaveChannel = async () => {
    localTracks.stop()
    localTracks.close()
    channel.sendMessage({ text: JSON.stringify({ 'type': 'user_left', 'uid': uid }) })
    await client.unpublish(localTracks)
    await channel.leave()
    await rtmClient.logout()
}

window.addEventListener('beforeunload', leaveChannel)
document.getElementById('buttonMic').addEventListener('click', toggleMic)
document.getElementById('volume-master').addEventListener('change', function (){
    let volume = document.getElementById('volume-master').value
    localTracks.setVolume(parseInt(volume))
})
document.getElementById('playback-btn').addEventListener('click', function () {
    if (document.getElementById('playback-btn').classList.contains('active'))
        localTracks.play()
    else
        localTracks.stop()
})

//document.getElementById(`kick-btn-${user.uid}`).addEventListener('click', function () {
//    var broadcast = false;
//    const selectedOptions = user.uid

//    // Validate input
//    const errors = [];

//    if (selectedOptions.length === 0) {
//        errors.push("กรุณาเลือกสถานีกระจายเสียง");
//    }
//    if (errors.length > 0) {
//        const errorHtml = errors.map(error => `<li>${error}</li>`).join("");
//        Swal.fire({
//            icon: 'error',
//            title: 'เกิดข้อผิดพลาด...',
//            html: `กรุณาแก้ไขข้อผิดพลาดต่อไปนี้<ul>${errorHtml}</ul>`,
//        });

//        return; // Stop the function if there are errors
//    }

//    var data = {
//        key: "1234567890",
//        key_multi: selectedOptions,
//        type_user: "multi",
//        broadcast: broadcast,
//    };

//    $.ajax({
//        type: "POST",
//        url: "/station/broadcast",
//        contentType: "application/json; charset=utf-8",
//        data: JSON.stringify(data),
//        success: function (response) {
//            if (response.success) {
//                Swal.fire({
//                    icon: 'success',
//                    title: 'สำเร็จ..',
//                    text: response.message,
//                    timer: 3000
//                }).then(() => {
//                    broadcast = !broadcast;
//                })
//            } else {
//                Swal.fire({
//                    icon: 'error',
//                    title: 'เกิดข้อผิดพลาด...',
//                    text: response.message,
//                    timer: 3000,
//                })
//            }
//        },
//        error: function (error) {
//            Swal.fire({
//                icon: 'error',
//                title: 'เกิดข้อผิดพลาด...',
//                text: error.responseText,
//            })
//        }
//    });
//});
//})

init()