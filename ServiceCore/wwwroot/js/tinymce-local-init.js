document.addEventListener("DOMContentLoaded", function () {
    tinymce.init({
        selector: 'textarea[name="Content"]',
        promotion: false,
        base_url: '/lib/tinymce',
        suffix: '.min',
        plugins: 'advlist autolink lists link image charmap preview anchor searchreplace visualblocks code fullscreen insertdatetime media table code help wordcount',
        toolbar: 'undo redo | blocks | bold italic backcolor | alignleft aligncenter alignright alignjustify | bullist numlist outdent indent | removeformat | help',
        menubar: false,
        skin: 'oxide-dark',
        content_css: 'dark',
        height: 400,
        setup: function (editor) {
            editor.on('change', function () {
                editor.save();
            });
        }
    });
});
