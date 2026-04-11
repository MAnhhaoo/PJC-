// Dùng một đối tượng Audio duy nhất
window.audioControl = new Audio();
window._audioEndedCallback = null;

window.playAudio = (url, dotnetRef) => {
    try {
        console.log("Đang phát file: " + url);
        if (!url) { console.warn("URL rỗng"); return; }
        window.audioControl.pause();
        window.audioControl.currentTime = 0;
        window.audioControl.src = url;
        window.audioControl.load();
        window.audioControl.onended = () => {
            if (dotnetRef) {
                dotnetRef.invokeMethodAsync('OnAudioEnded').catch(() => { });
            }
        };
        window.audioControl.onerror = (e) => {
            console.error("Lỗi audio: ", e);
            if (dotnetRef) {
                dotnetRef.invokeMethodAsync('OnAudioEnded').catch(() => { });
            }
        };
        window.audioControl.play().catch(e => {
            console.error("Không thể phát nhạc: ", e);
            if (dotnetRef) {
                dotnetRef.invokeMethodAsync('OnAudioEnded').catch(() => { });
            }
        });
    } catch (ex) {
        console.error("playAudio exception: ", ex);
        if (dotnetRef) {
            try { dotnetRef.invokeMethodAsync('OnAudioEnded').catch(() => { }); } catch (_) { }
        }
    }
};

window.stopAudio = () => {
    window.audioControl.pause();
    window.audioControl.currentTime = 0;
    window.audioControl.onended = null;
};

window.downloadFile = (url, filename) => {
    const a = document.createElement('a');
    a.href = url;
    a.download = filename;
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);
};

window.printQR = (imgUrl, name) => {
    const w = window.open('', '_blank');
    w.document.write('<html><head><title>QR - ' + name + '</title></head><body style="text-align:center;padding:40px;">');
    w.document.write('<h2>' + name + '</h2>');
    w.document.write('<img src="' + imgUrl + '" style="width:300px;height:300px;" onload="window.print();window.close();" />');
    w.document.write('<p>Quét mã QR để xem chi tiết nhà hàng</p>');
    w.document.write('</body></html>');
    w.document.close();
};
