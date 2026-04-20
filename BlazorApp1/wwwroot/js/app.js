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

// ===== Tour Map (Leaflet) =====
window._tourMap = null;
window._tourMarkers = [];
window._tourPolyline = null;

window._tourMapRetry = 0;
window.initTourMap = (elementId) => {
    const el = document.getElementById(elementId);
    if (!el) {
        if (window._tourMapRetry < 10) {
            window._tourMapRetry++;
            setTimeout(() => window.initTourMap(elementId), 300);
        }
        return;
    }
    window._tourMapRetry = 0;

    if (window._tourMap) {
        try { window._tourMap.remove(); } catch(e) {}
        window._tourMap = null;
    }

    window._tourMap = L.map(elementId).setView([10.762622, 106.660172], 13);
    L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
        attribution: '&copy; OpenStreetMap'
    }).addTo(window._tourMap);

    setTimeout(() => {
        if (window._tourMap) window._tourMap.invalidateSize();
    }, 400);
    setTimeout(() => {
        if (window._tourMap) window._tourMap.invalidateSize();
    }, 1000);
};

window.updateTourMapMarkers = (markers) => {
    if (!window._tourMap) return;

    function isValidCoord(lat, lng) {
        return typeof lat === 'number' && typeof lng === 'number'
            && isFinite(lat) && isFinite(lng)
            && lat >= -90 && lat <= 90
            && lng >= -180 && lng <= 180
            && (Math.abs(lat) > 0.0001 || Math.abs(lng) > 0.0001);
    }

    window._tourMarkers.forEach(m => m.remove());
    window._tourMarkers = [];
    if (window._tourPolyline) {
        window._tourPolyline.remove();
        window._tourPolyline = null;
    }

    if (!markers || markers.length === 0) {
        window._tourMap.setView([10.762622, 106.660172], 13);
        return;
    }

    const latlngs = [];
    let seq = 0;
    markers.forEach((m) => {
        if (isValidCoord(m.lat, m.lng)) {
            seq++;
            const icon = L.divIcon({
                className: '',
                html: '<div style="background:#1565C0;color:white;border-radius:50%;width:30px;height:30px;display:flex;align-items:center;justify-content:center;font-weight:bold;font-size:14px;border:2px solid white;box-shadow:0 2px 6px rgba(0,0,0,0.35);">' + seq + '</div>',
                iconSize: [30, 30],
                iconAnchor: [15, 15],
                popupAnchor: [0, -18]
            });
            const marker = L.marker([m.lat, m.lng], { icon: icon })
                .addTo(window._tourMap)
                .bindPopup('<b>' + seq + '. ' + m.name + '</b><br/>' + (m.address || ''));
            window._tourMarkers.push(marker);
            latlngs.push([m.lat, m.lng]);
        }
    });

    if (latlngs.length >= 2) {
        window._tourPolyline = L.polyline(latlngs, { color: '#1565C0', weight: 4, dashArray: '8,8' }).addTo(window._tourMap);
    }

    if (latlngs.length > 0) {
        window._tourMap.fitBounds(latlngs, { padding: [40, 40], maxZoom: 16 });
    } else {
        window._tourMap.setView([10.762622, 106.660172], 13);
    }

    window._tourMap.invalidateSize();
};

window.destroyTourMap = () => {
    if (window._tourMap) {
        window._tourMap.remove();
        window._tourMap = null;
    }
    window._tourMarkers = [];
    window._tourPolyline = null;
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

// ===== Analytics Maps =====
window._heatmapMap = null;
window._journeyMap = null;
window._heatLayer = null;
window._heatmapMarkers = [];
window._journeyMarkers = [];
window._journeyLines = [];

window.initAnalyticsHeatmap = (elementId) => {
    console.log('[Heatmap] initAnalyticsHeatmap called, elementId:', elementId);
    const el = document.getElementById(elementId);
    if (!el || el.offsetWidth === 0) {
        console.log('[Heatmap] Element not found or hidden, el:', !!el, 'width:', el?.offsetWidth);
        return false;
    }

    if (window._heatmapMap) {
        console.log('[Heatmap] Destroying old map');
        try { window._heatmapMap.remove(); } catch (e) { }
        window._heatmapMap = null;
    }
    (window._heatmapMarkers || []).forEach(m => m.remove());
    window._heatmapMarkers = [];
    window._heatLayer = null;

    window._heatmapMap = L.map(elementId).setView([10.762622, 106.660172], 12);
    L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
        attribution: '&copy; OpenStreetMap'
    }).addTo(window._heatmapMap);

    setTimeout(() => {
        if (window._heatmapMap) window._heatmapMap.invalidateSize();
    }, 400);
    setTimeout(() => {
        if (window._heatmapMap) window._heatmapMap.invalidateSize();
    }, 1000);
    console.log('[Heatmap] Map created successfully');
    return true;
};

window.initJourneyMap = (elementId) => {
    console.log('[Journey] initJourneyMap called, elementId:', elementId);
    const el = document.getElementById(elementId);
    if (!el || el.offsetWidth === 0) {
        console.log('[Journey] Element not found or hidden, el:', !!el, 'width:', el?.offsetWidth);
        return false;
    }

    if (window._journeyMap) {
        console.log('[Journey] Destroying old map');
        try { window._journeyMap.remove(); } catch (e) { }
        window._journeyMap = null;
    }
    (window._journeyMarkers || []).forEach(m => m.remove());
    window._journeyMarkers = [];
    (window._journeyLines || []).forEach(l => l.remove());
    window._journeyLines = [];

    window._journeyMap = L.map(elementId).setView([10.762622, 106.660172], 12);
    L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
        attribution: '&copy; OpenStreetMap'
    }).addTo(window._journeyMap);

    setTimeout(() => {
        if (window._journeyMap) window._journeyMap.invalidateSize();
    }, 400);
    setTimeout(() => {
        if (window._journeyMap) window._journeyMap.invalidateSize();
    }, 1000);
    console.log('[Journey] Map created successfully');
    return true;
};

window.updateHeatmap = (data, thresholds) => {
    console.log('[Heatmap] updateHeatmap called, data length:', data?.length, 'map exists:', !!window._heatmapMap);
    if (!window._heatmapMap) { console.log('[Heatmap] NO MAP - aborting'); return; }

    // Clear previous
    if (window._heatLayer) {
        window._heatmapMap.removeLayer(window._heatLayer);
        window._heatLayer = null;
    }
    (window._heatmapMarkers || []).forEach(m => m.remove());
    window._heatmapMarkers = [];

    if (!data || data.length === 0) { console.log('[Heatmap] No data - aborting'); return; }

    // Default thresholds if not provided
    if (!thresholds || thresholds.length === 0) {
        thresholds = [
            { min: 0, color: '#4CAF50', label: 'Rất thấp' },
            { min: 10, color: '#8BC34A', label: 'Thấp' },
            { min: 30, color: '#FFEB3B', label: 'Trung bình' },
            { min: 60, color: '#FF9800', label: 'Cao' },
            { min: 100, color: '#F44336', label: 'Rất cao' }
        ];
    }
    // Sort descending so we match the highest threshold first
    thresholds.sort((a, b) => b.min - a.min);

    function getColorForCount(count) {
        for (var i = 0; i < thresholds.length; i++) {
            if (count >= thresholds[i].min) return thresholds[i].color;
        }
        return thresholds[thresholds.length - 1].color;
    }

    function getLabelForCount(count) {
        for (var i = 0; i < thresholds.length; i++) {
            if (count >= thresholds[i].min) return thresholds[i].label || '';
        }
        return '';
    }

    console.log('[Heatmap] First item:', JSON.stringify(data[0]));

    // Build heatmap points: [lat, lng, intensity]
    const maxCount = Math.max(...data.map(d => d.count));
    const heatPoints = [];
    const bounds = [];

    data.forEach(d => {
        if (typeof d.lat === 'number' && typeof d.lng === 'number' &&
            (Math.abs(d.lat) > 0.0001 || Math.abs(d.lng) > 0.0001)) {
            const intensity = d.count / maxCount;
            heatPoints.push([d.lat, d.lng, intensity]);
            bounds.push([d.lat, d.lng]);

            // Colored pin marker based on visit count thresholds
            var pinColor = getColorForCount(d.count);
            var pinLabel = getLabelForCount(d.count);
            var icon = L.divIcon({
                className: '',
                html: '<div style="background:' + pinColor + ';color:#fff;border-radius:50%;width:32px;height:32px;display:flex;align-items:center;justify-content:center;font-weight:bold;font-size:12px;border:3px solid #fff;box-shadow:0 2px 6px rgba(0,0,0,0.4);">' + d.count + '</div>',
                iconSize: [32, 32],
                iconAnchor: [16, 16],
                popupAnchor: [0, -18]
            });
            var marker = L.marker([d.lat, d.lng], { icon: icon })
                .addTo(window._heatmapMap)
                .bindPopup('<b>' + d.name + '</b><br/>Lượt ghé: <b>' + d.count + '</b><br/>Mức: <span style="color:' + pinColor + ';font-weight:bold;">' + pinLabel + '</span>');
            window._heatmapMarkers.push(marker);
        }
    });

    console.log('[Heatmap] Valid points:', heatPoints.length, 'heatLayer available:', typeof L.heatLayer === 'function');

    if (heatPoints.length > 0 && typeof L.heatLayer === 'function') {
        window._heatLayer = L.heatLayer(heatPoints, {
            radius: 35,
            blur: 25,
            maxZoom: 17,
            max: 1.0,
            gradient: { 0.2: '#4CAF50', 0.4: '#8BC34A', 0.6: '#FFEB3B', 0.8: '#FF9800', 1.0: '#F44336' }
        }).addTo(window._heatmapMap);
    }

    if (bounds.length > 0) {
        window._heatmapMap.fitBounds(bounds, { padding: [50, 50], maxZoom: 15 });
    }
    window._heatmapMap.invalidateSize();
    console.log('[Heatmap] Update complete, markers:', window._heatmapMarkers.length);
};

window.showJourneys = (sessions) => {
    console.log('[Journey] showJourneys called, sessions:', sessions?.length, 'map exists:', !!window._journeyMap);
    if (!window._journeyMap) { console.log('[Journey] NO MAP - aborting'); return; }

    // Clear previous journey lines
    (window._journeyLines || []).forEach(l => l.remove());
    window._journeyLines = [];
    (window._journeyMarkers || []).forEach(m => m.remove());
    window._journeyMarkers = [];

    if (!sessions || sessions.length === 0) {
        console.log('[Journey] No sessions - resetting view');
        window._journeyMap.setView([10.762622, 106.660172], 12);
        return;
    }

    console.log('[Journey] First session:', JSON.stringify({ id: sessions[0].sessionId, pointCount: sessions[0].pointCount, points: sessions[0].points?.length }));

    const colors = ['#1565C0', '#E53935', '#43A047', '#FB8C00', '#8E24AA', '#00ACC1', '#D81B60', '#5E35B1', '#00897B', '#F4511E'];
    const allBounds = [];

    sessions.forEach((session, idx) => {
        if (!session.points || session.points.length < 2) return;
        const pts = session.points.map(p => [p.lat, p.lng]);
        const color = colors[idx % colors.length];

        const line = L.polyline(pts, {
            color: color,
            weight: 3,
            opacity: 0.8,
            dashArray: '6,4'
        }).addTo(window._journeyMap);

        const startTime = session.startTime ? new Date(session.startTime).toLocaleString('vi-VN') : '';
        const userLabel = session.guestLabel ? '🕶️ ' + session.guestLabel : (session.userId ? 'User #' + session.userId : 'Không xác định');
        line.bindPopup('<b>' + (session.tourName || 'Tour #' + session.tourId) + '</b><br/>' +
            'Người dùng: ' + userLabel + '<br/>' +
            'Session: ' + session.sessionId.substring(0, 8) + '...<br/>' +
            'Điểm GPS: ' + session.pointCount + '<br/>' +
            'Bắt đầu: ' + startTime);

        window._journeyLines.push(line);

        // Start marker
        const startIcon = L.divIcon({
            className: '',
            html: '<div style="background:' + color + ';color:white;border-radius:50%;width:24px;height:24px;display:flex;align-items:center;justify-content:center;font-size:11px;font-weight:bold;border:2px solid white;box-shadow:0 2px 4px rgba(0,0,0,0.3);">S</div>',
            iconSize: [24, 24], iconAnchor: [12, 12]
        });
        const sm = L.marker(pts[0], { icon: startIcon }).addTo(window._journeyMap);
        window._journeyMarkers.push(sm);

        // End marker
        const endIcon = L.divIcon({
            className: '',
            html: '<div style="background:' + color + ';color:white;border-radius:50%;width:24px;height:24px;display:flex;align-items:center;justify-content:center;font-size:11px;font-weight:bold;border:2px solid white;box-shadow:0 2px 4px rgba(0,0,0,0.3);">E</div>',
            iconSize: [24, 24], iconAnchor: [12, 12]
        });
        const em = L.marker(pts[pts.length - 1], { icon: endIcon }).addTo(window._journeyMap);
        window._journeyMarkers.push(em);

        pts.forEach(p => allBounds.push(p));
    });

    console.log('[Journey] Lines drawn:', window._journeyLines.length, 'markers:', window._journeyMarkers.length, 'bounds:', allBounds.length);
    if (allBounds.length > 0) {
        window._journeyMap.fitBounds(allBounds, { padding: [50, 50], maxZoom: 15 });
    }
    window._journeyMap.invalidateSize();
};

window.destroyAnalyticsMap = () => {
    if (window._heatmapMap) {
        try { window._heatmapMap.remove(); } catch (e) { }
        window._heatmapMap = null;
    }
    if (window._journeyMap) {
        try { window._journeyMap.remove(); } catch (e) { }
        window._journeyMap = null;
    }
    window._heatLayer = null;
    window._heatmapMarkers = [];
    window._journeyMarkers = [];
    window._journeyLines = [];
};

// ===== Live Visitors Map =====
window._liveMap = null;
window._liveMarkers = [];

window.initLiveVisitorsMap = (elementId) => {
    const el = document.getElementById(elementId);
    if (!el || el.offsetWidth === 0) return false;

    if (window._liveMap) {
        try { window._liveMap.remove(); } catch (e) { }
        window._liveMap = null;
    }
    window._liveMarkers = [];

    window._liveMap = L.map(elementId).setView([10.762622, 106.660172], 12);
    L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
        attribution: '&copy; OpenStreetMap'
    }).addTo(window._liveMap);

    setTimeout(() => { if (window._liveMap) window._liveMap.invalidateSize(); }, 400);
    setTimeout(() => { if (window._liveMap) window._liveMap.invalidateSize(); }, 1000);
    return true;
};

window.updateLiveVisitors = (visitors, perRestaurant, thresholds) => {
    if (!window._liveMap) return;

    (window._liveMarkers || []).forEach(m => m.remove());
    window._liveMarkers = [];

    if (!thresholds || thresholds.length === 0) {
        thresholds = [
            { min: 0, color: '#4CAF50' },
            { min: 3, color: '#8BC34A' },
            { min: 5, color: '#FFEB3B' },
            { min: 10, color: '#FF9800' },
            { min: 20, color: '#F44336' }
        ];
    }
    thresholds.sort((a, b) => b.min - a.min);

    function getColor(count) {
        for (var i = 0; i < thresholds.length; i++) {
            if (count >= thresholds[i].min) return thresholds[i].color;
        }
        return '#4CAF50';
    }

    const bounds = [];

    // Show restaurant pins with visitor counts
    if (perRestaurant && perRestaurant.length > 0) {
        perRestaurant.forEach(r => {
            if (Math.abs(r.latitude) < 0.0001 && Math.abs(r.longitude) < 0.0001) return;
            var color = getColor(r.visitorCount);
            var icon = L.divIcon({
                className: '',
                html: '<div style="background:' + color + ';color:#fff;border-radius:50%;width:36px;height:36px;display:flex;align-items:center;justify-content:center;font-weight:bold;font-size:14px;border:3px solid #fff;box-shadow:0 2px 8px rgba(0,0,0,0.4);">' + r.visitorCount + '</div>',
                iconSize: [36, 36],
                iconAnchor: [18, 18],
                popupAnchor: [0, -20]
            });
            var m = L.marker([r.latitude, r.longitude], { icon: icon })
                .addTo(window._liveMap)
                .bindPopup('<b>' + r.name + '</b><br/>Khách đang ở đây: <b>' + r.visitorCount + '</b>');
            window._liveMarkers.push(m);
            bounds.push([r.latitude, r.longitude]);
        });
    }

    // Show individual visitor dots
    if (visitors && visitors.length > 0) {
        visitors.forEach(v => {
            if (Math.abs(v.lat) < 0.0001 && Math.abs(v.lng) < 0.0001) return;
            var color = v.type === 'user' ? '#1565C0' : '#9E9E9E';
            var marker = L.circleMarker([v.lat, v.lng], {
                radius: 5,
                fillColor: color,
                color: '#fff',
                weight: 1,
                fillOpacity: 0.7
            })
            .addTo(window._liveMap)
            .bindPopup('<b>' + v.label + '</b><br/>Loại: ' + (v.type === 'user' ? '👤 Đã đăng ký' : '🕶️ Khách vãng lai'));
            window._liveMarkers.push(marker);
            bounds.push([v.lat, v.lng]);
        });
    }

    if (bounds.length > 0) {
        window._liveMap.fitBounds(bounds, { padding: [50, 50], maxZoom: 15 });
    }
    window._liveMap.invalidateSize();
};

window.destroyLiveMap = () => {
    if (window._liveMap) {
        try { window._liveMap.remove(); } catch (e) { }
        window._liveMap = null;
    }
    window._liveMarkers = [];
};
