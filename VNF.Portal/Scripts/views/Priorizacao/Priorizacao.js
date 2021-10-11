var Index = 0;
var PageSize = 30;

function selectRow(id) {
    $("#row" + id).siblings().removeClass("selecionado");
    $("#row" + id).toggleClass("selecionado");
    $("#hdfId").val(id);
}

function Edit(controller) {
    if ($("#hdfId").val() != "")
        window.location = url + controller + '/Edit/' + $("#hdfId").val();
}



$(document).ready(function () {
})

$("#demo-setting").on('click', function () {
    $(".demo").toggleClass("activate");
});

function LoadData() {
    txtPedido = $("#txtPedido").val();
    ddlItensRecebidos = $("#ItensRecebidos").val();
    ddlItensVencidos = $("#ItensVencidos").val();

    $.ajax({
        async: true,
        cache: false,
        type: 'GET',
        url: url + "Priorizacao/GetDataPriorizacao",
        dataType: 'html',
        data: {
            ptxtPedido: txtPedido,
            pddlItensRecebidos : ddlItensRecebidos,
            pddlItensVencidos : ddlItensVencidos
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
            $('#divGrid').html(oReturn);
            $("#demo-setting").trigger("click");
        }
    });
}

function Exportar() {
    txtPedido = $("#txtPedido").val();
    ddlItensRecebidos = $("#ItensRecebidos").val();
    ddlItensVencidos = $("#ItensVencidos").val();

    $.ajax({
        async: true,
        cache: false,
        type: 'GET',
        url: url + "Priorizacao/Exportar",
        dataType: 'html',
        data: {
            ptxtPedido: txtPedido,
            pddlItensRecebidos: ddlItensRecebidos,
            pddlItensVencidos: ddlItensVencidos
        },
        error: function (erro) {
            $.SmartMessageBox({
                title: "Erros",
                content: "Erro ao efetuar a priorização",
                buttons: '[Fechar]'
            });
        },
        success: function (oReturn) {
            window.location = url + 'files/export/' + oReturn;
        }
    });
}

function SalvarPrioridade() {
    txtNPedido = $("#txtNPedido").val();
    txtItem = $("#txtItem").val();
    txtDias = $("#txtDias").val();
    //Marcio Spinosa - 03/05/2018 - CR00008351
    var chkReportar;
    if (document.getElementById('chkReportar').checked)
        chkReportar = true;
    //Marcio Spinosa - 03/05/2018 - CR00008351 - Fim
    var erros = "";

    if (txtNPedido == "" || txtNPedido == 0)
        erros += "<p>Informar o número de pedido</p>"
    if (txtItem == "" || txtItem == 0)
        erros += "<p>Informar o item do pedido</p>"
    if (txtDias == "" || txtDias == 0)
        erros += "<p>Informar quantidade de dias</p>"

    if (erros != "") {
        $.smallBox({
            title: "Erros",
            content: erros,
            color: "#C46A69",
            icon: "fa fa-exclamation-circle",
            buttons: '[Fechar]',
            timeout: 4000
        });
        return;
    }

    $("#btnSalvarPrioridade").html("Aguarde...");
    $("#btnSalvarPrioridade").addClass("disabled");

    $.ajax({
        async: true,
        cache: false,
        type: 'GET',
        url: url + "Priorizacao/SalvarPrioridade",
        dataType: 'json',
        data: {
            ptxtNPedido: txtNPedido,
            ptxtItem: txtItem,
            ptxtDias: txtDias,
            penviaEmail: chkReportar

        },
        error: function (erro) {
            $.smallBox({
                title: "Erros",
                content: "Erro ao efetuar a priorização",
                color: "#C46A69",
                icon: "fa fa-exclamation-circle",
                buttons: '[Fechar]',
                timeout: 4000
            });
            $("#btnSalvarPrioridade").html("Incluir");

        },
        success: function (oReturn) {
            $("#txtNPedido").val('');
            $("#txtItem").val('');
            $("#txtDias").val('');
            $("#btnSalvarPrioridade").removeClass("disabled");
            $("#btnFecharModal").trigger('click');

            $("#btnSalvarPrioridade").html("Incluir");

            $.smallBox({
                title: "Alerta",
                content: "Priorização realizada com sucesso",
                timeout: 4000
            });
        }
    });
}
