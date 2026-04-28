function togglePassword() {
    const input = document.getElementById("passwordInput");
    const icon = document.querySelector(".toggle-password i");

    if (input.type === "password") {
        input.type = "text";
        icon.classList.remove("bi-eye");
        icon.classList.add("bi-eye-slash");
    } else {
        input.type = "password";
        icon.classList.remove("bi-eye-slash");
        icon.classList.add("bi-eye");
    }
}
document.querySelector("form").addEventListener("submit", function () {
    const btn = document.getElementById("loginBtn");
    btn.innerHTML = "Logging in...";
    btn.disabled = true;
});