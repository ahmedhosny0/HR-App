
    document.addEventListener('DOMContentLoaded', function () {
        $('#navall').show(); 
        $('#MainCategory').show();
        var body = document.body;
        var role = body.getAttribute('data-role');
        $('#liDB').hide();
        $('#VTransferUsers').hide();
    $('#VGetDeviceTime').hide();
    $('#VSetDeviceTime').hide();
    $('#VUsersData2').hide();
    $('#VUsersData').hide();
    $('#VGetAttendanceData').hide();
    $('#VAttendance').hide();
    $('#VMachineStatus').hide();
    $('#VDeleteUser').hide();
        $('#VDeleteInActive').hide(); 
        $('#VUsersData2').hide();
        //$('#exampleBranches').hide();
        $('#VGetAttendanceDataDelete').hide();
    if (role === 'BranchOwner') {
        // $('#liDBbefore').hide();
        $('#VAttendance').show();
        $('#MainCategory').hide();
    }
    else if (role === 'HeadOfficeHR')
    {
        $('#VAttendance').show();
    }
    else if (role === 'ITFood') {
        $('#liDB').show();
        $('#VTransferUsers').show();
    $('#VGetDeviceTime').show();
    $('#VSetDeviceTime').show();
    $('#VUsersData').show();
        $('#VAttendance').hide();
    $('#VMachineStatus').show();
    $('#liRichCut').hide();
    //$('#liFoulAlomer').hide();
    $('#liHeadOffice').hide();
    $('#liSUB_FRANCHISE').hide();
    $('#liTMT').hide();
            }
    else {
        $('#liDB').show();
        $('#liRichCut').show();
        //$('#liFoulAlomer').show();
        $('#liHeadOffice').show();
        $('#liSUB_FRANCHISE').show();
        $('#liTMT').show();
        if (role === 'AK' || role === 'FullAccess') {
                $('#VUsersData2').show();
               // $('#exampleBranches').show();
    $('#VGetAttendanceDataDelete').show();
        }
        $('#VGetAttendanceData').show();
    $('#VTransferUsers').show();
    $('#VGetDeviceTime').show();
    $('#VSetDeviceTime').show();
    $('#VUsersData').show();
    $('#VAttendance').show();
    $('#VMachineStatus').show();
    $('#VDeleteUser').show();
    $('#VDeleteInActive').show();
        }
    });
