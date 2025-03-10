document.addEventListener("DOMContentLoaded", function () {
  const mainImage = document.querySelector(".banner-img");
  const carouselImages = document.querySelectorAll(".carousel-item img");

  carouselImages.forEach((image) => {
    image.addEventListener("click", function () {
      mainImage.src = this.src;
    });
  });
});
