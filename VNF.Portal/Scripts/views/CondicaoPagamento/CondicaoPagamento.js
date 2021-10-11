function selectRow(id, pConPag, pNumDia) {
    $("#hdfConPag").val(pConPag);
    $("#hdfNumDia").val(pNumDia);

    $(".info").removeClass("info");
    $("#row_" + id).addClass("info");

    //$(".selecionado").removeClass("selecionado");
    //$("#row_" + id).addClass("selecionado");
}

function Salvar() {
    txtConPag = $("#txtConPag").val();
    txtNumDia = $("#txtNumDia").val();

    pNumDiaAtual = $("#hdfNumDia").val();

    var erros = "";

    if (txtConPag == "") {
        erros += "<p>Informar a descrição do Código SAP</p>"
    }
    if (txtNumDia == "") {
        erros += "<p>Informar o valor do Número de dias</p>"
    }

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

    var UrlParametro;
    var pData;

    var Editar = $("#IdEdit").val();
    if (Editar == 'EDITAR') {
        UrlParametro =  url + "CondicaoPagamento/EditarConPag";
        pData = {
            CONPAG: txtConPag,
            NUMDIA: txtNumDia,
            NUMDIAATUAL: pNumDiaAtual
        }
    }
    else {
        UrlParametro = url + "CondicaoPagamento/SalvarConPag";
        pData = {
            CONPAG: txtConPag,
            NUMDIA: txtNumDia
        }
    }

    $.ajax({
        async: true,
        cache: false,
        type: 'GET',
        url: UrlParametro,
        dataType: 'json',
        data: pData,
        error: function (erro) {
            $.smallBox({
                title: "Erros",
                content: "Erro ao efetuar a Condição de Pagamento",
                color: "#C46A69",
                icon: "fa fa-exclamation-circle",
                buttons: '[Fechar]',
                timeout: 4000
            });
        },
        success: function (oReturn) {
            $("#txtConPag").val('');
            $("#txtNumDia").val('');            

            $.smallBox({
                title: "Alerta",
                content: "Condição de Pagamento realizada com sucesso",
                timeout: 4000,
                color: "#739E73",
                icon: "fa fa-check-circle",
            });
        }
    });

}

function Editar(pEditar) {
    $("#idTitulo").text("Editar");

    pConPag = $("#hdfConPag").val();
    pNumDia = $("#hdfNumDia").val();

    $("#txtConPag").attr('disabled', true);
    $("#txtConPag").val(pConPag);
    $("#txtNumDia").val(pNumDia);

    $("#IdEdit").val(pEditar);
}

function Novo() {
    $("#idTitulo").text("Novo");

    $("#txtConPag").val('');
    $("#txtNumDia").val('');

    $("#hdfConPag").val('');
    $("#hdfNumDia").val('');

    $("#txtConPag").attr('disabled', false);
    $(".selecionado").removeClass("selecionado");
}