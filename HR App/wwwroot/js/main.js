document.getElementById("filterForm").addEventListener("submit", function () {

    localStorage.setItem("startDate", document.querySelector('input[name="startDate"]').value);
    localStorage.setItem("endDate", document.querySelector('input[name="endDate"]').value);
    //localStorage.setItem("branch", document.querySelector('select[name="Branch"]').value);

});

window.addEventListener("load", function () {

    const startDate = localStorage.getItem("startDate");
    const endDate = localStorage.getItem("endDate");
    const branch = localStorage.getItem("branch");

    if (startDate)
        document.querySelector('input[name="startDate"]').value = startDate;

    if (endDate)
        document.querySelector('input[name="endDate"]').value = endDate;

    //if (branch) {
    //    const select = document.querySelector('select[name="Branch"]');
    //    select.value = branch;

        // مهم جدًا مع select2
    //    if (typeof $ !== 'undefined') {
    //        $(select).trigger('change');
    //    }
    //}

});
$('.select2').select2({ placeholder: 'اختر الفرع', allowClear: true, width: '100%' });

$(document).on('select2:open', () => {
    setTimeout(() => document.querySelector('.select2-container--open .select2-search__field').focus(), 0);
});
