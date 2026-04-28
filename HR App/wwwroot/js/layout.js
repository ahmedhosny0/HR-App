document.addEventListener('DOMContentLoaded', function () {

    const toggleBtn = document.getElementById('navbarToggle');
    const navbarLeft = document.getElementById('navbarLeft');
    const navbarRight = document.getElementById('navbarRight');

    // =========================
    // Toggle main navbar (mobile)
    // =========================
    toggleBtn?.addEventListener('click', function (e) {
        e.stopPropagation();

        navbarLeft.classList.toggle('show');
        navbarRight.classList.toggle('show');

        // اقفل كل الدروب داون لما القائمة الرئيسية تتفتح/تقفل
        document.querySelectorAll('.nav-item.dropdown').forEach(i => {
            i.classList.remove('open');
        });
    });

    // =========================
    // Dropdown toggle (mobile only)
    // =========================
    document.querySelectorAll('.nav-item.dropdown > .nav-link')
        .forEach(link => {
            link.addEventListener('click', function (e) {

                if (window.innerWidth > 768) return;

                e.preventDefault();
                e.stopPropagation();

                const item = this.closest('.nav-item.dropdown');

                // اقفل الباقي
                document.querySelectorAll('.nav-item.dropdown')
                    .forEach(i => {
                        if (i !== item) {
                            i.classList.remove('open');
                        }
                    });

                // toggle الحالي (فتح + قفل)
                item.classList.toggle('open');
            });
        });

    // =========================
    // Close when clicking outside
    // =========================
    document.addEventListener('click', function (e) {

        if (!e.target.closest('.nav-item.dropdown') &&
            !e.target.closest('#navbarToggle')) {

            document.querySelectorAll('.nav-item.dropdown')
                .forEach(i => i.classList.remove('open'));
        }
    });

    // =========================
    // Optional: close on resize (fix stuck state)
    // =========================
    window.addEventListener('resize', function () {
        if (window.innerWidth > 768) {
            document.querySelectorAll('.nav-item.dropdown')
                .forEach(i => i.classList.remove('open'));
        }
    });

});