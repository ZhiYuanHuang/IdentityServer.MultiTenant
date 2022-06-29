window.addEventListener('load', function () {
    // 获取表单验证样式
    var forms = document.getElementsByClassName('needs-validation');
    // 循环并禁止提交
    var validation = Array.prototype.filter.call(forms, function (form) {
        form.addEventListener('submit', function (event) {
            if (form.checkValidity() === false) {
                event.preventDefault();
                event.stopPropagation();
            }
            form.classList.add('was-validated');
        }, false);
    });
}, false);