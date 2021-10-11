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
        url: url + "Parametros/GetDataParametro",
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
                timeout: 4000
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
    $("#idTitulo").text("Novo");
    $("#txtParametro").val('');
    $("#txtValor").val('');
    $("#txtdescricao").val('');//Marcio Spinosa - 07/05/2018 - CR00008351
    $("#hdfParametro").val('');
    $("#hdfValor").val('');
    $("#hdfDescricao").val('');//Marcio Spinosa - 07/05/2018 - CR00008351
    $("#txtParametro").attr('disabled', false);
}

function Atualizar() {
    window.location = url + 'Parametros/Index';
}

function SalvarParametro() {
    txtParametro = $("#txtParametro").val();
    txtValor = $("#txtValor").val();
    txtDescricao = $("#txtDescricao").val();//Marcio Spinosa - 07/05/2018 - CR00008351
    pValorAtual = $("#hdfValor").val();
    pDescricao = $("#hdfDescricao").val();//Marcio Spinosa - 07/05/2018 - CR00008351

    var erros = "";

    if (txtParametro == "" || txtParametro == 0) {
        erros += "<p>Informar a descrição do parâmetro</p>"
    }
    //if (txtValor == "" || txtValor == 0) {
    if (txtValor == "") {
        erros += "<p>Informar o valor do parâmetro</p>"
    }
    //Marcio Spinosa - 07/05/2018 - CR00008351
    if (txtDescricao == "") {
        txtDescricao = " "
    }
    //Marcio Spinosa - 07/05/2018 - CR00008351 - Fim
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
        UrlParametro = url + "Parametros/EditaParametro";
        pData = {
            ptxtParametro: txtParametro,
            ptxtValorAtual: pValorAtual,
            ptxtValorNovo: txtValor,
            ptxtDescricao: txtDescricao//Marcio Spinosa - 07/05/2018 - CR00008351
        }
    }    
    else {
        UrlParametro = url + "Parametros/SalvarParametro";
        pData = {
            ptxtParametro: txtParametro,
            ptxtValor: txtValor,
            ptxtDescricao: txtDescricao//Marcio Spinosa - 07/05/2018 - CR00008351
        }
    }

    Atualizar();//Marcio Spinosa - 07/05/2018 - CR00008351

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
                timeout: 4000
            });          
        },
        success: function (oReturn) {
            $("#txtParametro").val('');
            $("#txtValor").val('');
            $("#txtdescricao").val('');//Marcio Spinosa - 07/05/2018 - CR00008351
            $("#btnSalvarParametro").removeClass("disabled");
            $("#btnFecharModal").trigger('click');

            $.smallBox({
                title: "Alerta",
                content: "Parâmetro realizada com sucesso",
                timeout: 4000,
                color: "#739E73",
                icon: "fa fa-check-circle",
            });
        }
    });
}

function Editar(pEditar) {
    $("#idTitulo").text("Editar");
    
    pParametro = $("#hdfParametro").val();
    pValor = $("#hdfValor").val();
    pDescricao = $("#hdfDescricao").val();//Marcio Spinosa - 07/05/2018 - CR00008351

    $("#txtParametro").attr('disabled',true);
    $("#txtParametro").val(pParametro);
    $("#txtValor").val(pValor);
    $("#txtDescricao").val(pDescricao);//Marcio Spinosa - 07/05/2018 - CR00008351
    $("#IdEdit").val(pEditar);
}

function Excluir() {
    pParametro = $("#hdfParametro").val();
    pValor = $("#hdfValor").val();


    $.SmartMessageBox({
        title: "Excluir Parametro",
        content: "Deseja realmente excluir o parâmetro?",
        buttons: '[Não][Sim]'
    }, function (ButtonPressed) {
        if (ButtonPressed === "Sim") {
            $.ajax({
                async: true,
                cache: false,
                type: 'GET',
                url: url + "Parametros/ExcluirParametro",
                dataType: 'json',
                data: {
                    ptxtParametro: pParametro,
                    ptxtValor: pValor
                },
                error: function (erro) {
                    $.smallBox({
                        title: "Erros",
                        content: "Não foi possível completar sua solicitação!",
                        color: "#C46A69",
                        icon: "fa fa-exclamation-circle fadeInLeft animated",
                        buttons: '[Fechar]',
                        timeout: 4000
                    });
                },
                success: function (oReturn) {
                    $.smallBox({
                        title: "VNF NELES",
                        content: "Exclusão efetuado com sucesso.",
                        color: "#739E73",
                        icon: "fa fa-trash-o fadeInLeft animated",
                        buttons: '[Fechar]',
                        timeout: 4000
                    });
                }
            });
            Atualizar();//Marcio Spinosa - 07/05/2018 - CR00008351
        }
    });
   

}