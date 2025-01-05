// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.
// Store the original image source
const profileImagePreview = document.getElementById('profileImagePreview');
const originalImageSrc = profileImagePreview ? profileImagePreview.src : null;

document.addEventListener('DOMContentLoaded', () => {
    const tooltipTriggerList = document.querySelectorAll('[data-bs-toggle="tooltip"]')
    const tooltipList = [...tooltipTriggerList].map(tooltipTriggerEl => new bootstrap.Tooltip(tooltipTriggerEl))

    showAccordionErrors();

});

function showAccordionErrors() {
    document.querySelectorAll('.accordion-item').forEach((accordionItem) => {
        console.log("Found accordion item");
        // Check if there are validation errors inside the accordion item's form
        if (accordionItem.querySelector('.field-validation-error')) {
            console.log("Found accordion item with error");
            // Get the accordion header button
            const accordionButton = accordionItem.querySelector('.accordion-button');
            const accordionCollapse = accordionItem.querySelector('.accordion-collapse');

            // Open the accordion item
            accordionButton.classList.remove('collapsed');
            accordionButton.setAttribute('aria-expanded', 'true');
            accordionCollapse.classList.add('show');

            // Style the accordion header to indicate an error
            const errorIcon = `<i class="fa-solid fa-triangle-exclamation me-2"></i>`;
            accordionButton.innerHTML = errorIcon + accordionButton.innerHTML;
        }
    });
}

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