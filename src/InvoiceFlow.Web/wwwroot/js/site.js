function toggleDrawer() {
    var drawer = document.getElementById('side-drawer');
    var backdrop = document.getElementById('drawer-backdrop');
    if (drawer && backdrop) {
        drawer.classList.toggle('drawer-open');
        backdrop.classList.toggle('visible');
    }
}

function closeDrawer() {
    var drawer = document.getElementById('side-drawer');
    var backdrop = document.getElementById('drawer-backdrop');
    if (drawer && backdrop) {
        drawer.classList.remove('drawer-open');
        backdrop.classList.remove('visible');
    }
}