// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.
const tooltipTriggerList = document.querySelectorAll('[data-bs-toggle="tooltip"]')
const tooltipList = [...tooltipTriggerList].map(tooltipTriggerEl => new bootstrap.Tooltip(tooltipTriggerEl))


// Store the original image source
const originalImageSrc = document.getElementById('profileImagePreview').src;

function previewImage(event) {
    const imagePreview = document.getElementById('profileImagePreview');
    const file = event.target.files[0];

    if (file) {
        const reader = new FileReader();
        reader.onload = function (e) {
            imagePreview.src = e.target.result; // Set the preview image to the selected file
        };
        reader.readAsDataURL(file);
    } else {
        imagePreview.src = originalImageSrc; // Reset to original image if no file is chosen
    }
}