var Index = 0;
var PageSize = 30;

function selectRow(id) {
    $("#row" + id).siblings().removeClass("selecionado");
    $("#row" + id).toggleClass("selecionado");
    $("#hdfId").val(id);
}

$(document).ready(function () {

    /* BASIC ;*/
    var responsiveHelper_dt_basic = undefined;
    var responsiveHelper_datatable_fixed_column = undefined;
    var responsiveHelper_datatable_col_reorder = undefined;
    var responsiveHelper_datatable_tabletools = undefined;

    var breakpointDefinition = {
        tablet: 1024,
        phone: 480
    };

    /* COLUMN SHOW - HIDE */
    $('#dttFundicao').dataTable({
        "sDom": "<'dt-toolbar'<'col-xs-12 col-sm-6'f><'col-sm-6 col-xs-6 hidden-xs'C>r>" + "t",
        "bPaginate": false,
        "bInfo": false,
        "bScrollInfinite": true,
        "bScrollCollapse": true,
        "sScrollY": "200px",

        "preDrawCallback": function () {
            // Initialize the responsive datatables helper once.
            if (!responsiveHelper_datatable_col_reorder) {
                responsiveHelper_datatable_col_reorder = new ResponsiveDatatablesHelper($('#dttFundicao'), breakpointDefinition);
            }
        },
        "rowCallback": function (nRow) {
            responsiveHelper_datatable_col_reorder.createExpandIcon(nRow);
        },
        "drawCallback": function (oSettings) {
            responsiveHelper_datatable_col_reorder.respond();
        }
    });
})

$("#demo-setting").on('click', function () {
    $(".demo").toggleClass("activate");
});

function ConsultarDanfe() {
    var erros = "";
    var danfe = $("#txtDanfe").val();
    if (danfe == "")
        erros += "<p>-DANFE deve ser informado.</p>";
    if ($.trim(erros) != "") {
        $.smallBox({
            title: "Erros",
            content: erros,
            color: "#C46A69",
            icon: "fa fa-exclamation-circle",
            buttons: '[Fechar]',
            timeout: 4000
        })
    }
    else {
        $.ajax({
            url: url + 'Portaria/ConsultarDanfe',
            async: false,
            cache: false,
            type: 'GET',
            dataType: 'json',
            data: {
                pStart: Index,
                pLength: PageSize,
                pDanfe: danfe
            },
            error: function () {
                alert(erro);
            },
            success: function (oReturn) {

                if (oReturn.data.length == 0) {
                    if (Index == 0)
                        $("#dttNotas tfoot").hide();
                    //Index = undefined;
                } else {
                    $("#dttNotas tbody").append(oReturn.data);

                    if (oReturn.pagina >= PageSize) {
                        Index++;
                    } else {
                        //Index = undefined;
                        $("#dttNotas tfoot").hide();
                    }
                }
                $("#txtDanfe").val("");
            }
        });
    }
}

function validarForm() {
    var erros = "";

    var pasta = $("#txtPasta").val();
    var setor = $("#txtSetor").val();
    var transportadora = $("#txtTransportadora").val();
    var motorista = $("#txtMotorista").val();
    var placa = $("#txtPlaca").val();

    if (pasta == "")
        erros += "<p>-Pasta deve ser informada.</p>";
    if (setor == "")
        erros += "<p>-Setor deve ser informado.</p>";
    if (transportadora == "")
        erros += "<p>-Transportadora deve ser informada.</p>";
    if (motorista == "")
        erros += "<p>-Motorista deve ser informado.</p>";
    if (placa == "")
        erros += "<p>-Placa deve ser informada.</p>";

    if ($.trim(erros) != "") {
        $.smallBox({
            title: "Erros",
            content: erros,
            color: "#C46A69",
            icon: "fa fa-exclamation-circle",
            buttons: '[Fechar]',
            timeout: 4000
        })
        return false;
    }
    else {
        return true;
    }
}


$(function () {
    $("#btnNovaPasta").click(function () {
        $("#modal").load("Pasta", function () {
            $("#modal").modal();
        });
    });
});

$(function () {
    $("#btnVisualizarDetalhe").click(function () {
        var id = $("#hdfId").val();
        $("#modal").load("Pasta?pUnidade=FUND&pPasta=" + id, function () {
            $("#modal").modal();
        });
    });
});

function registrarChegada() {

    if (validarForm()) {

        var pasta = $("#txtPasta").val();
        var setor = $("#txtSetor").val();
        var transportadora = $("#txtTransportadora").val();
        var motorista = $("#txtMotorista").val();
        var placa = $("#txtPlaca").val();
        var notas = '';

        $('#dttNotas tbody tr').each(function () {
            if (notas != '')
                notas = notas + ';' + $(this).find('.chave').html();
            else
                notas = $(this).find('.chave').html();
        });

        $.ajax({
            url: url + 'Portaria/RegistrarChegada',
            async: false,
            cache: false,
            type: 'POST',
            dataType: 'json',
            data: {
                pUnidade: 'FUND',
                pPasta: pasta,
                pMotorista: motorista,
                pPlaca: placa,
                pSetor: setor,
                pTransportadora: transportadora,
                pNotas: notas
            },
            error: function (jqXHR, error, errorThrown) {
                $.smallBox({
                    title: "Erros",
                    content: jqXHR.responseText,
                    color: "#C46A69",
                    icon: "fa fa-exclamation-circle",
                    buttons: '[Fechar]',
                    timeout: 4000
                });
            },
            success: function (oReturn) {
                if (oReturn.data == "") {
                    $('#modal').modal('hide');
                    LoadData();
                }
                else {
                    $.smallBox({
                        title: "Erros",
                        content: oReturn.data,
                        color: "#C46A69",
                        icon: "fa fa-exclamation-circle",
                        buttons: '[Fechar]',
                        timeout: 4000
                    });
                }
            }
        });
    }
}

function registrarEntrada() {

    if (validarForm()) {

        var pasta = $("#txtPasta").val();

        $.ajax({
            url: url + 'Portaria/RegistrarEntrada',
            async: false,
            cache: false,
            type: 'POST',
            dataType: 'json',
            data: {
                pUnidade: 'FUND',
                pPasta: pasta
            },
            error: function (jqXHR, error, errorThrown) {
                $.smallBox({
                    title: "Erros",
                    content: jqXHR.responseText,
                    color: "#C46A69",
                    icon: "fa fa-exclamation-circle",
                    buttons: '[Fechar]',
                    timeout: 4000
                });
            },
            success: function (oReturn) {
                if (oReturn.data == "") {
                    $('#modal').modal('hide');
                    LoadData();
                }
                else {
                    $.smallBox({
                        title: "Erros",
                        content: oReturn.data,
                        color: "#C46A69",
                        icon: "fa fa-exclamation-circle",
                        buttons: '[Fechar]',
                        timeout: 4000
                    });
                }
            }
        });
    }
}

function registrarSaida() {

    if (validarForm()) {

        var pasta = $("#txtPasta").val();

        $.ajax({
            url: url + 'Portaria/RegistrarSaida',
            async: false,
            cache: false,
            type: 'POST',
            dataType: 'json',
            data: {
                pUnidade: 'FUND',
                pPasta: pasta
            },
            error: function (jqXHR, error, errorThrown) {
                $.smallBox({
                    title: "Erros",
                    content: jqXHR.responseText,
                    color: "#C46A69",
                    icon: "fa fa-exclamation-circle",
                    buttons: '[Fechar]',
                    timeout: 4000
                });
            },
            success: function (oReturn) {
                if (oReturn.data == "") {
                    $('#modal').modal('hide');
                    LoadData();
                }
                else {
                    $.smallBox({
                        title: "Erros",
                        content: oReturn.data,
                        color: "#C46A69",
                        icon: "fa fa-exclamation-circle",
                        buttons: '[Fechar]',
                        timeout: 4000
                    });
                }
            }
        });
    }
}

function ExibirMensagem() {
    var hdfMensagem = $("#hdfMensagem").val();

    if (hdfMensagem != "") {
        $.smallBox({
            title: "Alerta",
            content: hdfMensagem,
            timeout: 4000
        });
    }
}

function ExportarFundicao() {
    $("#divPostingPage").removeClass("hide");
    $("#divPostingPage").show();
    $('html, body').animate({ scrollTop: 0 }, 'slow');

    numeroPasta = $("#txtNumeroPasta").val();

    $.ajax({
        async: true,
        cache: false,
        type: 'POST',
        url: url + "Portaria/ExportarFundicao",
        dataType: 'html',
        data: {
            pNumeroPasta: numeroPasta
        },
        error: function () {
            $("#divPostingPage").addClass("hide");
        },
        success: function (oReturn) {
            $("#divPostingPage").addClass("hide");
            window.location = url + 'files/export/' + oReturn;
        }
    });
}

function LoadData() {

    $.ajax({
        async: true,
        cache: false,
        type: 'POST',
        url: url + "Portaria/LoadDataFundicao",
        dataType: 'json',
        data: {},
        error: function (erro) {
            alert(erro);
        },
        success: function (oReturn) {
            $("#dttFundicao tbody").empty();
            if (oReturn.data.length == 0) {
                $("#dttFundicao tfoot").hide();
            } else {
                $("#dttFundicao tbody").append(oReturn.data);
            }
        }
    });
}

function relatorioDivergencias() {

    var notas = '';

    $('#dttNotas tbody tr').each(function () {
        if (notas != '')
            notas = notas + ';' + $(this).find('.chave').html();
        else
            notas = $(this).find('.chave').html();
    });

    $.ajax({
        async: true,
        cache: false,
        type: 'POST',
        url: url + "Portaria/RelatorioDivergencias",
        dataType: 'json',
        data: {
            pNotas: notas
        },
        error: function (erro) {
            alert(erro);
        },
        success: function (oReturn) {
            //$("#dttFundicao tbody").empty();
            //if (oReturn.data.length == 0) {
            //    $("#dttFundicao tfoot").hide();
            //} else {
            //    $("#dttFundicao tbody").append(oReturn.data);
            //}
        }
    });
}