var label_status = document.getElementById("microphone-status");
var canvas_wave = document.getElementById("audio-waveform");

var audioCtx = new (window.AudioContext || window.webkitAudioContext)();
var stream;
var recorder;
var chunks = [];
var visualizer;

var lamejs = require('lamejs');
var mp3encoder = new lamejs.Mp3Encoder(1, 44100, 128); // mono, 44.1khz, 128kbps
var mp3Data = [];

// start recording
document.getElementById("start-recording").addEventListener("click", function () {
    navigator.mediaDevices.getUserMedia({ audio: true, video: false }).then(function (s) {
        stream = s;
        recorder = new MediaRecorder(stream);
        recorder.start();

        canvas_wave.style.display = "block";
        label_status.innerText = "สถานะอุปกรณ์รับเสียง : กำลังอัดเสียง";

        // visualize audio
        visualizer = audioCtx.createAnalyser();
        var source = audioCtx.createMediaStreamSource(stream);
        source.connect(visualizer);
        visualizer.fftSize = 2048;
        var bufferLength = visualizer.frequencyBinCount;
        var dataArray = new Uint8Array(bufferLength);
        visualizer.getByteTimeDomainData(dataArray);

        var canvas = document.getElementById("audio-waveform");
        var canvasCtx = canvas.getContext("2d");

        function draw() {
            var drawVisual = requestAnimationFrame(draw);
            visualizer.getByteTimeDomainData(dataArray);
            canvasCtx.fillStyle = "rgb(200, 200, 200)";
            canvasCtx.fillRect(0, 0, canvas.width, canvas.height);
            canvasCtx.lineWidth = 2;
            canvasCtx.strokeStyle = "rgb(0, 0, 0)";
            canvasCtx.beginPath();

            var sliceWidth = canvas.width * 1.0 / bufferLength;
            var x = 0;
            for (var i = 0; i < bufferLength; i++) {
                var v = dataArray[i] / 128.0;
                var y = v * canvas.height / 2;
                if (i === 0) {
                    canvasCtx.moveTo(x, y);
                } else {
                    canvasCtx.lineTo(x, y);
                }
                x += sliceWidth;
            }
            canvasCtx.lineTo(canvas.width, canvas.height / 2);
            canvasCtx.stroke();
        }
        draw();
        chunks = [];
        recorder.ondataavailable = function (e) {
            chunks.push(e.data);
            var reader = new FileReader();
            reader.readAsArrayBuffer(e.data);
            reader.onloadend = function () {
                var samples = new Int16Array(reader.result);
                var mp3buffer = mp3encoder.encodeBuffer(samples);
                mp3Data.push(mp3buffer);
            }
        }.catch(function (err) {
            label_status.innerText = "สถานะอุปกรณ์รับเสียง : " + err;
        });
    });
});

// stop recording
document.getElementById("stop-recording").addEventListener("click", function () {
    recorder.stop();
    stream.getTracks().forEach(function (track) { track.stop(); });
    label_status.innerText = "สถานะอุปกรณ์รับเสียง : หยุดการอัดเสียง";
    var mp3buffer = mp3encoder.flush();
    mp3Data.push(mp3buffer);
});

// play recording
document.getElementById("play-recording").addEventListener("click", function () {
    var blob = new Blob(chunks, { type: "audio/ogg; codecs=opus" });
    var audioURL = window.URL.createObjectURL(blob);
    var audio = new Audio(audioURL);
    audio.play();
    label_status.innerText = "สถานะอุปกรณ์รับเสียง : กำลังเล่นเสียง";

    audio.onended = function () {
        label_status.innerText = "สถานะอุปกรณ์รับเสียง : เล่นเสียงสำเร็จ";
    };
});

document.getElementById("save-recording").addEventListener("click", function () {
    var blob2 = new Blob(mp3Data, { type: "audio/mp3" });
    var url = URL.createObjectURL(blob2);
    var link = document.createElement("a");
    link.href = url;
    link.download = "recording.mp3";
    link.click();
});

// save recording
