document.addEventListener("DOMContentLoaded", function () {
  const avatar = document.getElementById("avatar");
  const dropdownMenu = document.getElementById("dropdown-menu");

  avatar.addEventListener("click", function (event) {
    dropdownMenu.style.display =
      dropdownMenu.style.display === "block" ? "none" : "block";
    event.stopPropagation(); // Ngăn sự kiện click lan ra ngoài
  });

  document.addEventListener("click", function (event) {
    if (!dropdownMenu.contains(event.target) && event.target !== avatar) {
      dropdownMenu.style.display = "none";
    }
  });
});
document
  .getElementById("avatarUpload")
  .addEventListener("change", function (event) {
    const file = event.target.files[0];
    if (file) {
      const reader = new FileReader();
      reader.onload = function (e) {
        document.getElementById("profile-avatar").src = e.target.result;
      };
      reader.readAsDataURL(file);
    }
  });
