
$(document).ready(function () {

    $("#defaultLogApplication").hide();
    $("#defaultLogService").hide();

    // DO NOT REMOVE : GLOBAL FUNCTIONS!
    pageSetUp();

    /*
    * PAGE RELATED SCRIPTS
    */

    $(".js-status-update a").click(function () {
        var selText = $(this).text();
        var $this = $(this);
        $this.parents('.btn-group').find('.dropdown-toggle').html(selText + ' <span class="caret"></span>');
        $this.parents('.dropdown-menu').find('li').removeClass('active');
        $this.parent().addClass('active');
    });


    /*
    * RUN PAGE GRAPHS
    */

    /* TAB 1: UPDATING CHART */
	var $chrt_border_color = "#efefef";
	var $chrt_grid_color = "#DDD"
	var $chrt_main = "#E24913";	
	var $chrt_second = "#6595b4";/* red       */	
	var $chrt_third = "#FF9F01";/* blue      */	
	var $chrt_fourth = "#7e9d3a";/* orange    */	
	var $chrt_fifth = "#BD362F";/* green     */	
	var $chrt_mono = "#000";/* dark red  */
    
	if ($("#updating-chart").length) {
	    $.ajax({
	        async: true,
	        cache: false,
	        type: 'POST',
	        url: url + "Home/GetChartRecebimentoNF",
	        dataType: 'json',
	        error: function () {
	            $("#divNotificacoes").empty();
	            $("#divNotificacoes").append("<label>Não foi possível carregar o gráfico de recebimentos NF</label>");
	        },
	        success: function (oReturn) {
	            var d = $.parseJSON(oReturn.DadosRecebimentoNF); //varDadosRecebimentoNF
	            var varLegendaRecebimentoNF = $.parseJSON(oReturn.LegendaRecebimentoNF);
	            var options = {
	                xaxis: {
	                    ticks: varLegendaRecebimentoNF
	                },
	                series: {
	                    lines: {
	                        show: true,
	                        lineWidth: 1,
	                        fill: true,
	                        fillColor: {
	                            colors: [{
	                                opacity: 0.1
	                            }, {
	                                opacity: 0.15
	                            }]
	                        }
	                    },
	                    //points: { show: true },
	                    shadowSize: 0
	                },
	                selection: {
	                    mode: "x"
	                },
	                grid: {
	                    hoverable: true,
	                    clickable: true,
	                    tickColor: $chrt_border_color,
	                    borderWidth: 0,
	                    borderColor: $chrt_border_color,
	                },
	                tooltip: true,
	                tooltipOpts: {
	                    content: "%y documentos processados"
	                },
	                colors: [$chrt_second],

	            };
	            var plot = $.plot($("#updating-chart"), [d], options);
	        }
	    });
	};
    
	$(function () {
	    $.ajax({
	        async: true,
	        cache: false,
	        type: 'POST',
	        url: url + "Home/GetChartDivergenciasAtivas",
	        dataType: 'json',
	        error: function () {
	            $("#divNotificacoes").empty();
	            $("#divNotificacoes").append("<label>Não foi possível carregar o gráfico de divergencias ativas</label>");
	        },
	        success: function (oReturn) {
	            var d = $.parseJSON(oReturn.DadosDivergenciasAtivas); //varDadosDivergenciasAtivas
	            var varLegendaDivergenciasAtivas = $.parseJSON(oReturn.LegendaDivergenciasAtivas);
	            var options = {
	                xaxis: {
	                    ticks: varLegendaDivergenciasAtivas
	                },
	                series: {
	                    lines: {
	                        show: true,
	                        lineWidth: 1,
	                        fill: true,
	                        fillColor: {
	                            colors: [{
	                                opacity: 0.1
	                            }, {
	                                opacity: 0.15
	                            }]
	                        }
	                    },
	                    //points: { show: true },
	                    shadowSize: 0
	                },
	                selection: {
	                    mode: "x"
	                },
	                grid: {
	                    hoverable: true,
	                    clickable: true,
	                    tickColor: $chrt_border_color,
	                    borderWidth: 0,
	                    borderColor: $chrt_border_color,
	                },
	                tooltip: true,
	                tooltipOpts: {
	                    content: "%y divergências ativas"
	                },
	                colors: [$chrt_main],

	            };

	            var plot = $.plot($("#statsChart"), [d], options);
	            $("#Divergencias").removeClass("active");
	        }
	    });
	});
    
    $(function () {

        $.ajax({
            async: true,
            cache: false,
            type: 'GET',
            url: url + "Home/GetNotificacoes",
            dataType: 'json',
            error: function () {
                $("#divNotificacoes").empty();
                $("#divNotificacoes").append("<label>Não foi possível carregar as notificações</label>");
            },
            success: function (oReturn) {
                var intMateriaisBloqueado = oReturn.MateriaisBloqueado;
                var intPedidosPrioritarios = oReturn.PedidosPrioritarios;
                var intDivergenciasAtivas = oReturn.DivergenciasAtivas;
                var intDocumentosPendentes = oReturn.DocumentosPendentes;
                var intProcessamentoFiscal = oReturn.ProcessamentoFiscal;
                
                $("#lblMateriaisBloqueados").text(intMateriaisBloqueado);
                $("#lblPedidosPrioritarios").text(intPedidosPrioritarios);
                $("#lblDivergenciasAtivas").text(intDivergenciasAtivas);
                $("#lblDocumentosPendentes").text(intDocumentosPendentes);
                $("#lblProcessamentoFiscal").text(intProcessamentoFiscal);

                VerificarCheckNotificacao("MateriaisBloqueado", intMateriaisBloqueado);
                VerificarCheckNotificacao("PedidosPrioritarios", intPedidosPrioritarios);
                VerificarCheckNotificacao("DivergenciasAtivas", intDivergenciasAtivas);
                VerificarCheckNotificacao("DocumentosPendentes", intDocumentosPendentes);
                VerificarCheckNotificacao("ProcessamentoFiscal", intProcessamentoFiscal);
            }
        });
    });

    function VerificarCheckNotificacao(name, qtd) {
        if (qtd == 0) {
            $("#li" + name).addClass("complete");
            $("#chk" + name).prop("checked", "checked");
        }
    }

    LoadDocumentosRecebidos();
    LoadLogApplication();
    LoadLogService();
});

function LoadDocumentosRecebidos() {
    $.ajax({
        async: true,
        cache: false,
        type: 'POST',
        url: url + "Home/GetDocumentosRecebidos",
        dataType: 'json',
        error: function () {
            $("#divNotificacoes").empty();
            $("#divNotificacoes").append("<label>Não foi possível carregar os documentos recebidos</label>");
        },
        success: function (oReturn) {
            //<tr id="defaultDocumentoRecebido">
            //    <td><a href="/Compras/Edit/0">VNF_TIPO_DOCUMENTO NF_IDE_NNF</a></td>
            //    <td>NF_NOME_EMITENTE</td>
            //    <td>NF_QTD_ITENS</td>
            //    <td>SITUACAO</td>
            //    <td>PRIORIDADE_ALTA</td>
            //</tr>

            $.each(oReturn.DocumentosRecebidos, function (key, documento) {
                // Clona o Documento Recebido Padrão
                var tdDocumento = $("#defaultDocumentoRecebido").clone();
                tdDocumento.attr("id", "tdDocumento"); // Troca id pra não ser mais o default

                var link = $(tdDocumento).find('td:nth-child(1) a');
                $(link).attr("href", $(link).attr("href") + '/' + documento.NFEID);
                $(link).text(documento.VNF_TIPO_DOCUMENTO + ' ' + documento.NF_IDE_NNF);
                
                $(tdDocumento).find('td:nth-child(2)').text(documento.NF_NOME_EMITENTE);
                $(tdDocumento).find('td:nth-child(3)').text(documento.NF_QTD_ITENS);
                $(tdDocumento).find('td:nth-child(4)').text(documento.SITUACAO);
                $(tdDocumento).find('td:nth-child(5)').text(documento.PRIORIDADE_ALTA);

                $('#tbDocumentosRecebidos tr:last').after(tdDocumento);
            });

            // Remove Documento Recebido Padrão
            $("#defaultDocumentoRecebido").remove();
        }
    });
}

function LoadLogApplication() {
    $.ajax({
        async: true,
        cache: false,
        type: 'POST',
        url: url + "Home/GetLogApplication",
        dataType: 'json',
        error: function () {
            $("#divNotificacoes").empty();
            $("#divNotificacoes").append("<label>Não foi possível carregar o log da aplicação</label>");
        },
        success: function (oReturn) {
            //<li class="message" id="defaultLogApplication">
            //    <img src="log_user" class="online" alt="log_user" onerror="this.src='@Url.Content("~/Images/Users/user_undefined_square.png")'">
            //    <div class="message-text">
            //        <time>
            //            log_date
            //        </time>
            //        <a href="#" class="username cursor-default">
            //            log_user
            //        </a>
            //        log_description &nbsp; log_icon
            //    </div>
            //</li>

            $.each(oReturn.LogApplication, function (key, log) {
                // Clona o LogApplication Padrão
                var liLogApplication = $("#defaultLogApplication").clone();
                liLogApplication.attr("id", "liLogApplication"); // Troca id pra não ser mais o default

                var img = $(liLogApplication).find('img');
                $(img).attr("src", log.log_user_img);
                $(img).attr("alt", log.log_user);

                var div = $(liLogApplication).find('div');
                $(div).find('time').text(log.log_date);
                $(div).find('a').text(log.UserNameBySamId);
                //$(div).contents().filter(function () {
                //    return this.nodeType == 3
                //}).each(function () {
                //    this.textContent = this.textContent.replace('log_description log_icon', log.log_description);
                //});
                $(div).append(log.log_description + '   ');
                $(div).append(log.log_icon);

                liLogApplication.css("display", "block");
                $('#lstLogApplication li:last').after(liLogApplication);
            });

            // Remove LogApplication Padrão
            $("#defaultLogApplication").remove();
        }
    });
}

function LoadLogService() {
    $.ajax({
        async: true,
        cache: false,
        type: 'POST',
        url: url + "Home/GetLogService",
        dataType: 'json',
        error: function () {
            $("#divNotificacoes").empty();
            $("#divNotificacoes").append("<label>Não foi possível carregar o log do serviço</label>");
        },
        success: function (oReturn) {
            //<li id="defaultLogService">
            //    log_icon
            //    <div class="smart-timeline-time">
            //        <small>log_date</small>
            //    </div>
            //    <div class="smart-timeline-content">
            //        <p class="cursor-default">
            //            log_title
            //        </p>
            //        <p>
            //            log_description
            //        </p>
            //    </div>
            //</li>

            $.each(oReturn.LogService, function (key, log) {
                // Clona o LogService Padrão
                var liLogService = $("#defaultLogService").clone();
                liLogService.attr("id", "liLogService"); // Troca id pra não ser mais o default

                $(liLogService).html($(liLogService).html().replace('log_icon', log.log_icon));
                $(liLogService).html($(liLogService).html().replace('log_date', log.log_date));
                $(liLogService).html($(liLogService).html().replace('log_title', log.log_title));
                $(liLogService).html($(liLogService).html().replace('log_description', log.log_description));

                liLogService.css("display", "block");
                $('#lstLogService li:last').after(liLogService);
            });

            // Remove LogService Padrão
            $("#defaultLogService").remove();
        }
    });
}