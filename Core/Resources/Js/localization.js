'use strict'
var localization = (function ($) {

	var addButton = function () {
		var button = $('<div class="button">Localization</div>').appendTo(getContainer());
		button.click(addPopup);
	};

	var addPopup = function () {

		var container = getContainer();
		$.get('/_localization/popup.html', function (result) {

			container.append(result);

			var content = container.find('div.content');
			var popup = container.find('div.popup');
			var close = container.find('div.close');
			var tabs = container.find('div.tabs li');

			// align popup 
			var left = ($(window).width() - content.width()) / 2;
			var top = ($(window).height() - content.height()) / 2;

			content.css({ 'left': left, 'top': top });

			// close popup on click or esc
			close.click(function () {
				popup.remove();
			});

			$(window).bind('keydown', function (e) {
				if (e.keyCode == 27)
					popup.remove();
			});

			// switch tabs
			tabs.click(function () {

				$(this).addClass('selected');

				switch ($(this).text()) {
					case 'Resources':
						$('div.tabs li#import').removeClass('selected');
						addResourcesTab();
						break;
					case 'Import/Export':
						$('div.tabs li#resources').removeClass('selected');
						addImportTab();
						break;
				};
			});

			addResourcesTab();
		});
	};

	var addResourcesTab = function () {

		var container = getContainer().find('div.tab');

		container.html('');

		container._busy(
			$.get('/_localization/resources.html', function (result) {

				container.append(result);

				loadResources();
				$('div#create input[type="button"]').click(createLanguage);

			})
		);
	};

	var addImportTab = function () {

		var container = getContainer().find('div.tab');

		container.html('');

		container._busy(
			$.get('/_localization/import.html', function (result) {

				container.append(result);

				$.getJSON('/_localization/api/cultures', function (result) {

					$.each(result, function () {
						$(buildHtml('option', this.toString(), { 'value': this.toString() })).appendTo($('#culture'));
					});

					$('input#import').click(function () {

						var d = $.Deferred();
						container.find('#status div')._busy(d);
						container.find('#status label').text('Import in progress...');

						$('#import-frame').load(function () {
							d.resolve();
							container.find('#status label').text('Import finished.');
						});
					});
				});
			})
		);
	};

	var loadResources = function () {

		var culture = $('#culture');

		culture.change(function () {
			tree(culture.val());
		});

		culture.html('');

		culture._busy(
			$.getJSON('/_localization/api/cultures', function (result) {

				if (result.length > 0) {
					$.each(result, function () {
						$(buildHtml('option', this.toString(), { 'value': this.toString() })).appendTo(culture);
					});

					tree(culture.val());
				}
				else
					$('#tree').html('No languages. To create new language enter name (for example ru-ru) and press "create".');
			})
		);
	};

	var createLanguage = function () {

		var container = getContainer();
		var culture = $('div#create input[type="text"]').val();

		container.find('#toolbar')._busy(
			$.ajax({
				type: 'POST',
				url: '/_localization/api/create',
				data: 'culture=' + culture,
				success: function (result) {
					if (result != '') {
						if ($('#culture option[value="' + culture + '"]').length == 0)
							$(buildHtml('option')).val(result).text(result).appendTo($('#culture'));
					}
				}
			})
		);
	};

	var tree = function (culture) {

		if (culture != null) {

			var container = getContainer().find('div#tree');

			container.html('');

			container._busy(
				$.getJSON('/_localization/api/tree?culture=' + culture, function (result) {

					parseTree(result, container);

					var tree = $('ul.tree');

					tree.find('li span.folder').click(collapse);
					tree.find('li span.label').click(function () {

						$('ul.tree li span.label').removeClass('selected');
						$(this).addClass('selected');

						table(culture, $(this).attr('scope'));
					});

					tree.find('li span.label').first().click();

				})
			);

			var parseTree = function (treeNode, container) {

				var ul = $(buildHtml('ul', { 'class': 'tree' })).appendTo(container);

				$.each(treeNode, function () {

					var li = $(buildHtml('li')).appendTo(ul);

					$(buildHtml('span', { 'class': this.Children.length > 0 ? 'folder' : 'file' })).appendTo(li);
					$(buildHtml('span', this.Label, { 'class': 'label' + (!this.Translated || this.Children.length > 0 ? ' untranslated' : ''), 'scope': this.Scope })).appendTo(li);

					parseTree(this.Children, li);
				});
			};

			var collapse = function () {

				$(this).toggleClass('collapsed');

				if ($(this).hasClass('collapsed'))
					$(this).parent().find('ul.tree').slideUp('fast');
				else
					$(this).parent().find('ul.tree').slideDown('fast');

				return false;
			};
		}
	};

	var table = function (culture, scope) {

		if (culture != null) {

			var container = getContainer().find('div#table');
			container.html('');

			container._busy(
				$.getJSON('/_localization/api/table?culture=' + culture + '&scope=' + scope, function (result) {

					var table = $(buildHtml('table', { 'class': 'resources-table' })).appendTo(container);

					// headers
					var row = $(buildHtml('tr', { 'class': 'header' })).appendTo(table);
					$(buildHtml('th', 'Text')).appendTo(row);
					$(buildHtml('th', 'Translation')).appendTo(row);
					$(buildHtml('th')).appendTo(row);

					// resources
					for (var i = 0; i < result.length; i++) {

						var row = $(buildHtml('tr')).appendTo(table);
						$(buildHtml('td', result[i].Text, { 'class': 'text' })).appendTo(row);
						$(buildHtml('td', result[i].Translation, { 'class': 'translation' })).appendTo(row);

						var op = $(buildHtml('td', { 'class': 'op' })).appendTo(row);

						var edit = $(buildHtml('a', 'Edit', {
							'href': '#',
							'key': result[i].Key,
							'prev': i == 0 ? '' : result[i - 1].Key,
							'next': i + 1 == result.length ? '' : result[i + 1].Key,
							'scope': result[i].Scope
						})).appendTo(op)

						op.append('&nbsp;');

						var del = $(buildHtml('a', 'Delete', {
							'href': '#',
							'key': result[i].Key
						})).appendTo(op);

						edit.click(function () {
							editTranslation($(this).attr('key'));
							return false;
						});

						del.click(function () {
							deleteTranslation($(this).attr('key'));
							return false;
						});
					};

				})
			);
		};
	};

	var editTranslation = function (id) {

		var container = getContainer().find('div#table');
		container.find('.resources-table').hide();

		var translation;

		$(container).bind('keydown', function (e) {

			if (!$('#translation').is(':focus')) {

				if (e.keyCode == 37)
					move($('a[key="' + id + '"]').attr('prev'));
				else if (e.keyCode == 39)
					move($('a[key="' + id + '"]').attr('next'));
			};
		});

		var move = function (key) {

			if (key != '') {

				if (translation != $('#translation').val())
					save();

				id = key;
				edit();
			}
		};

		var edit = function () {

			var href = $('a[key="' + id + '"]');
			var text = $(href.closest('tr').find('td').get(0)).html();
			var scope = href.attr('scope').length > 50
				? "..." + href.attr('scope').substring(href.attr('scope').length - 50)
				: href.attr('scope');

			translation = $(href.closest('tr').find('td').get(1)).html();

			$('input#id').val(id);
			$('#scope').html($('#culture').val() + ' translation for: ' + scope);
			$('#text').html(text);
			$('#translation').val(translation);

			$('#translation').focus();

			displayHint(text);
		};

		var save = function () {

			var key = $('input#id').val();
			var t = $('#translation').val();

			$.ajax({
				type: 'POST',
				url: '/_localization/api/edit',
				data: 'id=' + key + '&translation=' + t,
				success: function () {
					var href = $('a[key="' + key + '"]');
					$(href.closest("tr").find('td').get(1)).html(t);
				}
			});
		};

		var displayHint = function (text) {

			var hint = $('#hint');

			hint.html('');
			$.getJSON('/_localization/api/hint?culture=' + $('#culture').val() + '&text=' + text, function (result) {

				if (result.length > 0) {

					var count = $.inArray(translation, result) == -1 ? result.length : result.length - 1;
					var width = (parseInt(hint.css('max-width')) - 8 * count) / count;

					$.each(result, function () {
						if (this != translation) {

							var span = $(buildHtml('span', this, { 'title': this })).appendTo(hint);

							if (span.width() > width) {

								span.text('');
								span.width(0);

								for (var i = 0; i < this.length; i++) {
									span.text(span.text() + this[i]);
									if (span.width() > width) {
										span.text(span.text().substring(0, span.text().length - 3) + '...');
										break;
									};
								};
							};
						};
					});

					hint.find('span').click(function () {
						$('#translation').val($(this).attr('title'));
					});
				};
			});
		};

		container._busy(
			$.get('/_localization/edit.html', function (result) {

				var editContainer = $(result).appendTo(container);

				// move next/prev
				$('a#prev').click(function () {
					move($('a[key="' + id + '"]').attr('prev'));
					return false;
				});

				$('a#next').click(function () {
					move($('a[key="' + id + '"]').attr('next'));
					return false;
				});

				$('input#save').click(function () {
					save();
					container.find('.resources-table').show();
					editContainer.remove();
					$(container).unbind('keydown');
				});

				$('input#cancel').click(function () {
					container.find('.resources-table').show();
					editContainer.remove();
					$(container).unbind('keydown');
				});

				edit();
			})
		);
	};

	var deleteTranslation = function (id) {

		$.ajax({
			type: 'POST',
			url: '/_localization/api/delete',
			data: 'id=' + id,
			success: function () {
				$('a[key="' + id + '"]').closest("tr").hide('fast');
			}
		});
	};

	var getContainer = function () {

		var container = $('div#localization');

		if (container.length == 0)
			container = $('<div id="localization"/>').appendTo('body');

		return container;
	};

	var buildHtml = function (tag, html, attrs) {

		if (typeof (html) != 'string') {
			attrs = html;
			html = null;
		};

		var h = '<' + tag;
		if (attrs)
			for (var attr in attrs) {
				if (attrs[attr] === false) continue;
				h += ' ' + attr + '="' + attrs[attr] + '"';
			};

		return h += html ? ">" + html + "</" + tag + ">" : "/>";
	};

	return {
		init: function () {
			addButton();
		}
	};
})(jQuery);

jQuery.fn.extend({
	_busy: function (deferred) {

		var element = this;
		var div = $('<div class="busy"><img src="/_localization/loading.gif"/></div>');

		var pos = element.css('position');
		if (pos == 'static')
			element.css('position', 'relative');

		element.append(div);

		var img = div.find('img');
		img.css('position', 'relative').css('top', (element.height() - img.height()) / 2);

		div.show();

		deferred.then(function () {
			div.remove();
			if (pos == 'static')
				element.css('position', pos);
		});
	}
});