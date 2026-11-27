(() => {
    const notice = document.getElementById('approved-offline');
    if (!notice) return;
    const update = () => { notice.hidden = navigator.onLine; };
    window.addEventListener('online', update);
    window.addEventListener('offline', update);
    update();
})();
