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
    $('#dttTabela').dataTable({
        "sDom": "<'dt-toolbar'<'col-xs-12 col-sm-6'f><'col-sm-6 col-xs-12 hidden-xs'l>r>" +
                       "t" +
                       "<'dt-toolbar-footer'<'col-sm-6 col-xs-12 hidden-xs'i><'col-xs-12 col-sm-6'p>>",
        "autoWidth": true,
        "preDrawCallback": function () {
            // Initialize the responsive datatables helper once.
            if (!responsiveHelper_dt_basic) {
                responsiveHelper_dt_basic = new ResponsiveDatatablesHelper($('#dttTabela'), breakpointDefinition);
            }
        },
        "rowCallback": function (nRow) {
            responsiveHelper_dt_basic.createExpandIcon(nRow);
        },
        "drawCallback": function (oSettings) {
            responsiveHelper_dt_basic.respond();
        }
    });

    /* END COLUMN SHOW - HIDE */

})

function Atualizar() {
    window.location = url + 'RegimeEspecial/Index';
}

function RegimeEspecial() {
    if ($("#hdfId").val() != "") {
        $("#btnRegimeEspecial").attr("data-toggle", "modal");
        $("#btnRegimeEspecial").attr("data-target", "#modalNovaPasta");

        contador = 0;
        arrItensNCM = [];
        $("#tblItens tbody").empty();
        $("#txtItens").val("");

    }
    else {
        $.SmartMessageBox({
            title: "Erros",
            content: "Selecione uma nota",
            buttons: '[Fechar]'
        });
    }
}

function selectRow(id, re) {
    $("#row_" + id).siblings().removeClass("selecionado");
    $("#row_" + id).toggleClass("selecionado");
    $("#hdfId").val(id);
    $("#hdfRE").val(re);
    $("#txtEmail").val($("#tdEmail_row_" + id).text());
    IdForencedor = $("#tdIdFornecedor_row_" + id).text();
}

function ExportarNF() {

    $.ajax({
        async: true,
        cache: false,
        type: 'GET',
        url: url + "RegimeEspecial/ExportarNF",
        dataType: 'html',
        data: {
        },
        error: function (erro) {
            $.smallBox({
                title: "Erros",
                content: "Não foi possível concluir sua solicitação.",
                color: "#C46A69",
                icon: "fa fa-exclamation-circle",
                buttons: '[Fechar]',
                timeout: 4000
            });
        },
        success: function (oReturn) {
                window.location = url + 'files/export/' + oReturn;
            }
    });
}

function AdicionarNCM() {
    arrItensNCM.push($("#txtItens").val());
    $("#tblItens tbody").empty();
    for (var i = 0; i < arrItensNCM.length; i++) {
        $("#tblItens tbody").append("<tr id='trItens" + arrItensNCM[i] + "'><td style='width:30%'><button type='button' class='fa  fa-times' onclick='RemoveNCM(&#39;" + arrItensNCM[i] + "&#39;)'><span class=''></span></button></td><td>" + arrItensNCM[i] + "</td></tr>");
    }
    $("#txtItens").val("");
    contador++;
}

function RemoveNCM(id) {
    var index = 0;
    for (var i = 0; i < arrItensNCM.length; i++) {
        if (arrItensNCM[i] == id) {
            break;
        }
        index++;
    }

    if (index > -1) {

        $("#trItens" + arrItensNCM[index]).remove();

        arrItensNCM.splice(index, 1);
    }
    contador--;
}

function SalvarNCM() {
    txtCNPJ = $("#hdfId").val();
    txtRE = $("#hdfRE").val();
    var itens = "";
    var virgula = "";

    for (i = 0; i < arrItensNCM.length; i++) {
        itens += virgula + arrItensNCM[i];
        virgula = ","
    }

    $.ajax({
        async: true,
        cache: false,
        type: 'GET',
        url: url + "RegimeEspecial/InserirNCM",
        dataType: 'json',
        data: {
            ptxtRE: txtRE,
            pCNPJ: txtCNPJ,
            pArrItens: itens

        },
        error: function (erro) {
            $.smallBox({
                title: "Erros",
                content: "Erro ao atualizar o registro!",
                color: "#C46A69",
                icon: "fa fa-exclamation-circle",
                buttons: '[Fechar]',
                timeout: 4000
            });
        },
        success: function (oReturn) {
            ExibirMensagem(oReturn.mensagem);
            $("#btnFecharNCM").trigger("click");
        }
    });

}