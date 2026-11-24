document.addEventListener('click', function (e) {
    var a = e.target.closest && e.target.closest('a[href]');
    if (!a) return;
    var url = new URL(a.href, location.href);
    if (url.origin === location.origin && url.pathname.toLowerCase() === '/login/logout') {
        e.preventDefault();
        document.getElementById('formLogout').submit();
    }
});
