var Index = 0;
var PageSize = 30;
var arrDvivergencia = [];

function SelectRowCodLog(nfe, id, codlog) {
    $("#" + id + codlog).siblings().removeClass("selecionado");
    $("#" + id + codlog).toggleClass("selecionado");
    $("#hfIdNfe").val(id);
    $("#hfCodLog").val(codlog);
    $("#btnAnular").removeClass("disabled");
    $("#btnCancelar").removeClass("disabled");
    $("#btnVerificar").removeClass("disabled");
    $("#btnVisualizar").removeClass("disabled");
}

function JustificativaDetalhe() {
    $("#divJustificativaDetalhe").removeClass("hide");
    $("#divJustificativaDetalhe").html("Justificativa</br><select id='dropDownJustificativa' style='width: 350px; '><option>Selecione...</option><option>ANULAÇÃO EM MASSA</option><option>DEVOLUCAO DO MATERIAL</option><option>ERRO DE SISTEMA</option><option>ERRO NA REGRA DE VALIDACAO</option>option>ERRO NO VALIDADOR</option>option>VALIDACAO APOS RECEBIMENTO</option></select></br></br>Detalhe</br><textarea name='textArea' id='textArea'maxlength='50' style='width: 350px; height: 70px;'></textarea>");
    $("#divJustificativaDetalhe").dialog({
        height: 270,
        width: 400,
        modal: true,
        resizable: false,
        dialogClass: "no-close",
        buttons: {
            Fechar: function () {
                $(this).dialog("close");
            },
            Anular: function () {
                $(this).dialog(Anular());
                $(this).dialog("close");
            }
        }
    });
}

function AddDvivergencia() {
    if ($("#ddlTipoDivergencia option:selected").text() == "") {
    } else {
        arrDvivergencia[arrDvivergencia.length] = $("#ddlTipoDivergencia option:selected").text();
        $("#ddlTipoDivergencia option:selected").remove();
        $("#btnFilterDivergencia").removeClass("btn-default");
        $("#btnFilterDivergencia").addClass("btn-primary");
    }
}

function FilterDivergencia() {
    $("#divFilterDivergencia").removeClass("hide");
    $("#divFilterDivergencia").dialog({
        height: 400,
        width: 800,
        modal: true,
        resizable: false,
        dialogClass: "no-close",
        buttons: {
            Fechar: function () {
                $(this).dialog("close");
            }
        }
    });
    $("#dttFilterDivergencia tbody").empty();
    for (var i = 0; i < arrDvivergencia.length; i++) {
        $("#dttFilterDivergencia tbody").append("<tr id='trDivergencia" + arrDvivergencia[i].replace(/\s+/g, '') + "'><td>" + arrDvivergencia[i] + "</td><td><button type='button' class='btn btn-default btn-xs' onclick='RemoveDivergencia(&#39;" + arrDvivergencia[i] + "&#39;)'><span class='glyphicon glyphicon-remove'></span></button></td></tr>");
    }
}

function RemoveDivergencia(id) {
    var index = 0;
    for (var i = 0; i < arrDvivergencia.length; i++) {
        if (arrDvivergencia[i] == id) {
            break;
        }
        index++;
    }
    if (index > -1) {
        $("#trDivergencia" + arrDvivergencia[index].replace(/\s+/g, '')).remove();
        $("#ddlTipoDivergencia").append("<option value='" + arrDvivergencia[index] + "'>" + arrDvivergencia[index] + "</option>");
        arrDvivergencia.splice(index, 1);
    }
    if (arrDvivergencia.length == 0) {
        $("#btnFilterDivergencia").removeClass("btn-primary");
        $("#btnFilterDivergencia").addClass("btn-default");
    }
    SortSelectOption("ddlDivergencia");
}

function FilterData() {
    $("#dttTabela tbody").empty();
    $("#dttTabela tfoot").show();
    LoadData();
    
}

function Visualizar() {//ok
    hfIdNfe = $("#hfIdNfe").val();
    window.location = url + 'Associacao/Edit/' + hfIdNfe;
}

function Cancelar() {//ok
    $("#btnCancelar").addClass("disabled");
    $.ajax({
        async: true,
        cache: false,
        type: 'GET',
        url: url + "Nfe/CancelarDIVE",
        dataType: 'json',
        data: {
            pIdNfe: hfIdNfe.value
        },
        error: function (erro) {
            $.smallBox({
                title: "Erros",
                content: "Não foi possível completar sua solicitação!",
                color: "#C46A69",
                icon: "fa fa-exclamation-circle",
                buttons: '[Fechar]',
                timeout: 4000
            });
            $("#btnCancelar").removeClass("disabled");
        },
        success: function (oReturn) {
            $.smallBox({
                title: "Alerta",
                content: "Cancelamento efetuado com sucesso.",
                color: "#739E73",
                icon: "fa fa-check-circle",
                timeout: 4000
            });
            $("#btnCancelar").removeClass("disabled");
        }
    });
}

function Anular() {//ok
    hfIdNfe = $("#hfIdNfe").val();
    hfCodLog = $("#hfCodLog").val();
    $.ajax({
        async: true,
        cache: false,
        type: 'GET',
        url: url + "Nfe/AnularDIVE",
        dataType: 'json',
        data: {
            NfeId: hfIdNfe,
            CodLog: hfCodLog,
            Justificativa: dropDownJustificativa.value,
            Detalhe: textArea.value
        },
        error: function (erro) {
            $.smallBox({
                title: "Erros",
                content: "Não foi possível completar sua solicitação!",
                color: "#C46A69",
                icon: "fa fa-exclamation-circle",
                buttons: '[Fechar]',
                timeout: 4000
            });
        },
        success: function () {
            $.smallBox({
                title: "Alerta",
                content: "Anulação efetuada com sucesso.",
                color: "#739E73",
                icon: "fa fa-check-circle",
                timeout: 4000
            });
        }
    });
}

function Verificar() {//ok
    $("#btnVerificar").addClass("disabled");
    $.ajax({
        async: true,
        cache: false,
        type: 'GET',
        url: url + "Nfe/VerificarDIVE",
        dataType: 'json',
        data: {
            pIdNfe: hfIdNfe.value
        },
        error: function (erro) {
            $.smallBox({
                title: "Erros",
                content: "Não foi possível completar sua solicitação!",
                color: "#C46A69",
                icon: "fa fa-exclamation-circle",
                buttons: '[Fechar]',
                timeout: 4000
            });
            $("#btnVerificar").removeClass("disabled");
        },
        success: function (oReturn) {
            $.smallBox({
                title: "Alerta",
                content: "Verificação efetuada com sucesso!",
                color: "#739E73",
                icon: "fa fa-check-circle",
                timeout: 4000
            });
            $("#btnVerificar").removeClass("disabled");
        }
    });
}

function LoadData() {//ok
    if (
        $("#Situacao").val() == "" &&
        $("#Motivo").val() == "" &&
        $("#txtCodigoComprador").val() == "" &&
        $("#txtNumeroPO").val() == "" &&
        $("#txtDocE").val() == "" &&
        $("#txtDataEmissaoAte").val() == "" &&
        $("#txtDataEmissaoDe").val() == "" &&
        $("#txtDataDivergenciaDe").val() == "" &&
        $("#txtDataDivergenciaAte").val() == ""
       )
    {
        $.SmartMessageBox({
            title: "Erros",
            content: "Preencha os campos de busca!",
            buttons: '[Fechar]'
        });
        return;
    }
    Situacao = $("#Situacao").val();
    Motivo = $("#Motivo").val();
    txtCodigoComprador = $("#txtCodigoComprador").val();
    txtNumeroPO = $("#txtNumeroPO").val();
    txtDocE = $("#txtDocE").val();
    txtDataEmissaoAte = $("#txtDataEmissaoAte").val();
    txtDataEmissaoDe = $("#txtDataEmissaoDe").val();
    txtDataDivergenciaDe = $("#txtDataDivergenciaDe").val();
    txtDataDivergenciaAte = $("#txtDataDivergenciaAte").val();
    var Concatena = '';
    for (var i = 0; i < arrDvivergencia.length; i++) {
        Concatena += arrDvivergencia[i] + ";";
    }
    $.ajax({
        async: true,
        cache: false,
        type: 'GET',
        url: url + "Nfe/GetDataDivergencia",
        dataType: 'html',
        data: {
            txtDocE: txtDocE,
            Motivo: Motivo,
            Situacao: Situacao,
            txtNumeroPO: txtNumeroPO,
            txtCodigoComprador: txtCodigoComprador,
            txtDataEmissaoDe: txtDataEmissaoDe,
            txtDataEmissaoAte: txtDataEmissaoAte,
            txtDataDivergenciaDe: txtDataDivergenciaDe,
            txtDataDivergenciaAte: txtDataDivergenciaAte,
            arrDivergencia: Concatena
        },
        error: function (erro) {
            $.smallBox({
                title: "Erros",
                content: "Não foi possível completar sua solicitação!",
                color: "#C46A69",
                icon: "fa fa-exclamation-circle",
                buttons: '[Fechar]',
                timeout: 4000
            });
        },
        success: function (oReturn) {
            $('#divGrid').html(oReturn);
        }
    });
}

function ExportarDIVE() {//ok
    $("#divPostingPage").removeClass("hide");
    $("#divPostingPage").show();
    $('html, body').animate({ scrollTop: 0 }, 'slow');
    Situacao = $("#Situacao").val();
    Motivo = $("#Motivo").val();
    txtCodigoComprador = $("#txtCodigoComprador").val();
    txtNumeroPO = $("#txtNumeroPO").val();
    txtDocE = $("#txtDocE").val();
    txtDataEmissaoAte = $("#txtDataEmissaoAte").val();
    txtDataEmissaoDe = $("#txtDataEmissaoDe").val();
    txtDataDivergenciaDe = $("#txtDataDivergenciaDe").val();
    txtDataDivergenciaAte = $("#txtDataDivergenciaAte").val();
    var Concatena = '';
    for (var i = 0; i < arrDvivergencia.length; i++) {
        Concatena += arrDvivergencia[i] + ";";
    }
    $.ajax({
        async: true,
        cache: false,
        type: 'GET',
        url: url + "Nfe/ExportarDIVE",
        dataType: 'html',
        data: {
            txtDocE: txtDocE,
            Motivo: Motivo,
            Situacao: Situacao,
            txtNumeroPO: txtNumeroPO,
            txtCodigoComprador: txtCodigoComprador,
            txtDataEmissaoDe: txtDataEmissaoDe,
            txtDataEmissaoAte: txtDataEmissaoAte,
            txtDataDivergenciaDe: txtDataDivergenciaDe,
            txtDataDivergenciaAte: txtDataDivergenciaAte,
            arrDivergencia: Concatena
        },
        error: function (erro) {
            $.smallBox({
                title: "Erros",
                content: "Não foi possível completar sua solicitação!",
                color: "#C46A69",
                icon: "fa fa-exclamation-circle",
                buttons: '[Fechar]',
                timeout: 4000
            });
        },
        success: function (oReturn) {
            $("#divPostingPage").addClass("hide");
            window.location = url + 'files/export/' + oReturn;
        }
    });
}

$(document).ready(function () {
    pageSetUp();
})


$("#demo-setting").on('click', function () {
    $(".demo").toggleClass("activate");
});

function setTable() {
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
    $('#dttTabela').dataTable({
        "sDom": "<'dt-toolbar'<'col-xs-12 col-sm-6'f><'col-sm-6 col-xs-12 hidden-xs'l>r>" +
                "t" +
                "<'dt-toolbar-footer'<'col-sm-6 col-xs-12 hidden-xs'i><'col-xs-12 col-sm-6'p>>",
        "bPaginate": true,
        "bInfo": false,
        "bScrollInfinite": true,
        "bScrollCollapse": true,
        "sScrollY": "200px",

        "preDrawCallback": function () {
            // Initialize the responsive datatables helper once.
            if (!responsiveHelper_datatable_col_reorder) {
                responsiveHelper_datatable_col_reorder = new ResponsiveDatatablesHelper($('#dttTabela'), breakpointDefinition);
            }
        },
        "rowCallback": function (nRow) {
            responsiveHelper_datatable_col_reorder.createExpandIcon(nRow);
        },
        "drawCallback": function (oSettings) {
            responsiveHelper_datatable_col_reorder.respond();
        }
    });

    /* END COLUMN SHOW - HIDE */

    $(document).ready(function () {
        //var table = $('#dttTabela').dataTable();

    });


    $(window).scroll(function () {
        if ($(window).scrollTop() >= 70) {
            $('.navigation-wrap').addClass('fixed');
        }
        else {
            $('.navigation-wrap').removeClass('fixed');
        }
        if ($(window).scrollTop() >= 108) {
            $('.dt-toolbar').addClass('toolfixed');
        }
        else {
            $('.dt-toolbar').removeClass('toolfixed');
        }
        if ($(window).scrollTop() >= 150) {
            $('.dataTables_scrollHeadInner').addClass('theadfixed');
        }
        else {
            $('.dataTables_scrollHeadInner').removeClass('theadfixed');
        }
    });
}