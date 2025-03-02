document.addEventListener("DOMContentLoaded", function () {
  const avatar = document.getElementById("profileDropdown");
  const dropdownMenu = document.querySelector(".dropdown-menu"); // Menu dropdown

  // Khi nhấn vào avatar, bật/tắt dropdown
  avatar.addEventListener("click", function (event) {
    event.stopPropagation(); // Ngăn chặn sự kiện click lan ra ngoài
    dropdownMenu.classList.toggle("show");
  });

  // Khi nhấn vào bất kỳ đâu trong body, kiểm tra nếu không phải avatar thì ẩn dropdown
  document.body.addEventListener("click", function (event) {
    if (
      !avatar.contains(event.target) &&
      !dropdownMenu.contains(event.target)
    ) {
      dropdownMenu.classList.remove("show");
    }
  });
});
