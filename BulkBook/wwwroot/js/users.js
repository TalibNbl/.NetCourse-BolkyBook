var userDataTable;

$(document).ready(function () {
    loadDataTable();
});

function loadDataTable() {
    userDataTable = new DataTable('#tblData', {
        ajax: '/admin/users/getall',
        columns: [
            { data: 'name', "width": "25%" },
            { data: 'email', "width": "15%" },
            { data: 'phoneNumber', "width": "10%" },
            {
                data: 'role', "width": "10%", "render": function (data) {
                    return '<span class="badge bg-secondary">' + (data || '') + '</span>';
                }
            },
            {
                data: null, "width": "25%", "render": function (data, type, row) {
                    var today = new Date().getTime();
                    var lockout = row.lockoutEnd ? new Date(row.lockoutEnd).getTime() : 0;
                    var isLocked = lockout > today;

                    return `<div class="d-flex gap-2 justify-content-end">
                    <a onclick="LockUnlock('${row.id}')" class="btn btn-sm ${isLocked ? 'btn-danger' : 'btn-success'}">
                        <i class="bi bi-${isLocked ? 'lock' : 'unlock'}-fill"></i> ${isLocked ? 'Unlock' : 'Lock'}
                    </a>
                    <a href="/admin/users/RoleManagment?userId=${row.id}" class="btn btn-sm btn-outline-secondary">
                        <i class="bi bi-person-badge"></i> Role
                    </a>
                    <a href="/admin/users/ChangePassword?userId=${row.id}" class="btn btn-sm btn-outline-danger">
                        <i class="bi bi-key-fill"></i> Password
                    </a>
                </div>`;
                }
            }
        ]
    });
}

function LockUnlock(id) {
    $.ajax({
        type: "POST",
        url: '/admin/users/LockUnlock',
        data: JSON.stringify(id),
        contentType: "application/json",
        success: function (data) {
            if (data.success) {
                toastr.success(data.message);
                userDataTable.ajax.reload();
            } else {
                toastr.error(data.message);
            }
        }
    });
}