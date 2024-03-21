// Call the dataTables jQuery plugin
$(document).ready(function () {
  $("#dataTable").DataTable({
    // ellipsis for long text
    columnDefs: [
      {
        targets: [0, 1, 2, 3, 4, 5, 6, 7],
        render: $.fn.dataTable.render.ellipsis(20),
      },
    ],
  });
});
