/// <reference path="../../libs/jquery-2.1.1.min.js" />

$(window).scroll(function () {
    if ($(window).scrollTop() + $(window).height() == $(document).height()) {
    }
});

function LoadData() {
    $.ajax({
        async: true,
        cache: false,
        type: 'GET',
        url: url + "J1B1N_CFOP_Escriturar/GetData",
        dataType: 'json',
        data: {
        },
        error: function (erro) {
            $.smallBox({
                title: "Erros",
                content: "Não foi possível concluir sua solicitação.",
                color: "#C46A69",
                icon: "fa fa-exclamation-circle",
                buttons: '[Fechar]',
                timeout: 1000
            });
        },
        success: function (oReturn) {

            if (oReturn.data.length == 0) {

                $("#dttTabela tfoot").hide();
                ;
            } else {
                $("#dttTabela tbody").append(oReturn.data);
            }
        }
    });
}

function Novo() {
    $("#idItem").text("Novo");
    $("#txtCFOP").val('');
    $("#txtCFOPEscriturar").val('');
    $("#hdfCFOPEscriturar").val('');
    $("#txtCFOPEscriturar").attr('disabled', false);
}

function Atualizar() {
    window.location = url + 'J1B1N_CFOP_Escriturar/Index';
}

function SalvarCFOP() {
    txtCFOP = $("#txtCFOP").val();
    txtCFOPEscriturar = $("#txtCFOPEscriturar").val();
    ptxtCFOPEscrituraAntigo = $("#hdfCFOPEscriturar").val();

    var erros = "";

    if (txtCFOP == "" || txtCFOP == 0) {
        erros += "<p>Informar o CFOP</p>"
    }

    if (txtCFOPEscriturar == "" || txtCFOPEscriturar == 0) {
        erros += "<p>Informar a descrição do CFOP à escriturar</p>"

    }

    if (erros != "") {
        $.smallBox({
            title: "Erros",
            content: erros,
            color: "#C46A69",
            icon: "fa fa-exclamation-circle",
            buttons: '[Fechar]',
            timeout: 1000
        });
        return;
    }

    var UrlParametro;
    var pData;

    var Editar = $("#idItem").val();
    if (Editar == 'EDITAR') {
        UrlParametro = url + "J1B1N_CFOP_Escriturar/Editar";
        pData = {
            pCFOP: txtCFOP,
            pCfopEscriturar: txtCFOPEscriturar
        }
    }
    else {
        UrlParametro = url + "J1B1N_CFOP_Escriturar/Salvar";
        pData = {
            pCFOP: txtCFOP,
            pCfopEscriturar: txtCFOPEscriturar
        }
    }

    //Atualizar();//Marcio Spinosa - 07/05/2018 - CR00008351

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
                content: "Erro ao efetuar o parâmetro",
                color: "#C46A69",
                icon: "fa fa-exclamation-circle",
                buttons: '[Fechar]',
                timeout: 1000
            });
        },
        success: function (oReturn) {
            $("#txtCFOP").val('');
            $("#txtCFOPEscriturar").val('');
            $("#btnSalvarParametro").removeClass("disabled");
            $("#btnFecharModal").trigger('click');

            $.smallBox({
                title: "Alerta",
                content: "Parâmetro realizada com sucesso",
                timeout: 1000,
                color: "#739E73",
                icon: "fa fa-check-circle",
            });
            Atualizar();
        }
    });
}

function Editar(pEditar) {
    $("#idItem").text("Editar");

    pCFOP = $("#hdfIdCFOP").val();
    pCFOPEscriturar = $("#hdfCFOPEscriturar").val();
    $("#txtIdItem").attr('disabled', true);
    $("#txtCFOP").val(pCFOP);
    $("#txtCFOPEscriturar").val(pCFOPEscriturar);
    $("#idItem").val(pEditar);
}

function Excluir() {
    pCFOP = $("#hdfIdCFOP").val();

    $.SmartMessageBox({
        title: "Excluir CFOP",
        content: "Deseja realmente excluir o CFOP?",
        buttons: '[Não][Sim]'
    }, function (ButtonPressed) {
        if (ButtonPressed === "Sim") {
            $.ajax({
                async: true,
                cache: false,
                type: 'GET',
                url: url + "J1B1N_CFOP_Escriturar/Excluir",
                dataType: 'json',
                data: {
                    pCFOP: pCFOP
                },
                error: function (erro) {
                    $.smallBox({
                        title: "Erros",
                        content: "Não foi possível completar sua solicitação!",
                        color: "#C46A69",
                        icon: "fa fa-exclamation-circle fadeInLeft animated",
                        buttons: '[Fechar]',
                        timeout: 1000
                    });
                },
                success: function (oReturn) {
                    $.smallBox({
                        title: "VNF NELES",
                        content: "Exclusão efetuado com sucesso.",
                        color: "#739E73",
                        icon: "fa fa-trash-o fadeInLeft animated",
                        buttons: '[Fechar]',
                        timeout: 1000
                    });
                }
            });
            Atualizar();
        }
    });
    

}