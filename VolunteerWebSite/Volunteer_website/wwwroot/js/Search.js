document.addEventListener("DOMContentLoaded", function () {
  const searchBtn = document.getElementById("search-btn");
  const searchOverlay = document.getElementById("search-overlay");
  const closeSearch = document.getElementById("close-search");

  // Hiển thị search
  searchBtn.addEventListener("click", function (event) {
    event.preventDefault();
    searchOverlay.style.display = "flex";
  });

  // Đóng search khi nhấn nút X
  closeSearch.addEventListener("click", function () {
    searchOverlay.style.display = "none";
  });

  // Đóng search khi click ra ngoài
  searchOverlay.addEventListener("click", function (event) {
    if (event.target === searchOverlay) {
      searchOverlay.style.display = "none";
    }
  });
});
