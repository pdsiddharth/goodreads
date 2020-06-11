"use strict";
// <copyright file="filter-bar.tsx" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>
var __extends = (this && this.__extends) || (function () {
    var extendStatics = function (d, b) {
        extendStatics = Object.setPrototypeOf ||
            ({ __proto__: [] } instanceof Array && function (d, b) { d.__proto__ = b; }) ||
            function (d, b) { for (var p in b) if (b.hasOwnProperty(p)) d[p] = b[p]; };
        return extendStatics(d, b);
    };
    return function (d, b) {
        extendStatics(d, b);
        function __() { this.constructor = d; }
        d.prototype = b === null ? Object.create(b) : (__.prototype = b.prototype, new __());
    };
})();
Object.defineProperty(exports, "__esModule", { value: true });
var React = require("react");
var react_northstar_1 = require("@fluentui/react-northstar");
var react_icons_northstar_1 = require("@fluentui/react-icons-northstar");
var Icon_1 = require("@fluentui/react/lib/Icon");
var icons_1 = require("@uifabric/icons");
var popup_menu_wrapper_1 = require("../../components/popup-menu/popup-menu-wrapper");
var react_i18next_1 = require("react-i18next");
var helper_1 = require("../../helpers/helper");
require("../../styles/filter-bar.css");
var FilterBar = /** @class */ (function (_super) {
    __extends(FilterBar, _super);
    function FilterBar(props) {
        var _this = _super.call(this, props) || this;
        _this.resize = function () {
            if (window.innerWidth !== _this.state.screenWidth) {
                _this.setState({ screenWidth: window.innerWidth });
            }
        };
        /**
        *Sets state of 'Type' filter item when checkbox value changes.
        *@param typeValues Array of 'post type' checkboxes with updated user selection
        */
        _this.onTypeCheckboxStateChange = function (typeValues) {
            _this.setState({ typeList: typeValues });
            _this.props.onTypeCheckboxStateChange(typeValues);
        };
        /**
        *Sets state of 'Shared by' filter item when checkbox value changes.
        *@param sharedByValues Array of 'authors' checkboxes with updated user selection
        */
        _this.onSharedByCheckboxStateChange = function (sharedByValues) {
            _this.setState({ sharedByList: sharedByValues });
            _this.props.onSharedByCheckboxStateChange(sharedByValues);
        };
        /**
        *Sets state of 'Tags' filter item when checkbox value changes.
        *@param tagsValues Array of 'tags' checkboxes with updated user selection
        */
        _this.onTagsCheckboxStateChange = function (tagsValues) {
            _this.setState({ tagsList: tagsValues });
            _this.props.onTagsStateChange(tagsValues);
        };
        /**
        *Sets state of selected sort by item.
        *@param selectedSortBy Selected 'sort by' value
        */
        _this.onSortByStateChange = function (selectedSortBy) {
            _this.setState({ selectedSortBy: selectedSortBy });
            _this.props.onSortByStateChange(selectedSortBy);
        };
        /**
        *Sets search text.
        *@param event Event object for input
        */
        _this.onSearchStateChange = function (event) {
            _this.setState({ searchText: event.target.value });
            _this.props.onFilterSearchChange(event.target.value);
        };
        /**
        *Removes all filters and hides filter bar.
        *@param event Event object for input
        */
        _this.onCloseIconClick = function (event) {
            if (_this.state.searchText.trim().length > 0) {
                _this.setState({ searchText: "" });
            }
            if (_this.state.sharedByList.filter(function (sharedBy) { return sharedBy.isChecked; }).length) {
                var updatedList = _this.state.sharedByList.map(function (sharedBy) { sharedBy.isChecked = false; return sharedBy; });
                _this.setState({ sharedByList: updatedList });
            }
            if (_this.state.tagsList.filter(function (tag) { return tag.isChecked; }).length) {
                var updatedList = _this.state.tagsList.map(function (tag) { tag.isChecked = false; return tag; });
                _this.setState({ tagsList: updatedList });
            }
            if (_this.state.typeList.filter(function (postType) { return postType.isChecked; }).length) {
                var updatedList = _this.state.typeList.map(function (postType) { postType.isChecked = false; return postType; });
                _this.setState({ typeList: updatedList });
            }
            _this.setState({ selectedSortBy: _this.state.sortBy[0].value });
            _this.props.onFilterBarCloseClick();
        };
        icons_1.initializeIcons();
        _this.localize = _this.props.t;
        var postTypes = helper_1.getLocalizedPostTypes(_this.localize).map(function (postType) {
            return {
                key: parseInt(postType.id),
                checkboxLabel: React.createElement(react_northstar_1.Flex, { vAlign: "center" },
                    React.createElement(react_northstar_1.Status, { styles: { backgroundColor: postType.color } }),
                    "\u00A0",
                    React.createElement(react_northstar_1.Text, { content: postType.name, title: postType.name })),
                title: postType.name,
                isChecked: false
            };
        });
        var sortBy = helper_1.getLocalizedSortBy(_this.localize).map(function (sortBy) { return { key: sortBy.id, label: sortBy.name, value: sortBy.id, name: sortBy.name }; });
        _this.state = {
            selectedSortBy: sortBy[0].value,
            typeList: postTypes,
            sharedByList: _this.props.sharedByAuthorList.map(function (value, index) {
                return { isChecked: false, key: index, title: value, checkboxLabel: React.createElement(react_northstar_1.Text, { content: value }) };
            }),
            tagsList: _this.props.tagsList.map(function (value, index) {
                return { isChecked: false, key: index, title: value, checkboxLabel: React.createElement(react_northstar_1.Text, { content: value }) };
            }),
            sortBy: sortBy,
            searchText: "",
            screenWidth: 800
        };
        return _this;
    }
    FilterBar.prototype.componentDidMount = function () {
        window.addEventListener("resize", this.resize.bind(this));
        this.resize();
    };
    FilterBar.prototype.componentWillReceiveProps = function (nextProps) {
        if (nextProps.sharedByAuthorList !== this.props.sharedByAuthorList) {
            this.setState({
                sharedByList: nextProps.sharedByAuthorList.map(function (value, index) {
                    return { isChecked: false, key: index, title: value, checkboxLabel: React.createElement(react_northstar_1.Text, { content: value }) };
                })
            });
        }
        if (nextProps.tagsList !== this.props.tagsList) {
            this.setState({
                tagsList: nextProps.tagsList.map(function (value, index) {
                    return { isChecked: false, key: index, title: value, checkboxLabel: React.createElement(react_northstar_1.Text, { content: value }) };
                })
            });
        }
    };
    /**
    * Renders the component
    */
    FilterBar.prototype.render = function () {
        if (this.props.isVisible) {
            return (React.createElement(react_northstar_1.Flex, null,
                this.state.screenWidth > 750 && React.createElement(react_northstar_1.Flex, { gap: "gap.small", vAlign: "center", className: "filter-bar-wrapper" },
                    React.createElement("div", { className: "searchbar-wrapper" },
                        React.createElement(react_northstar_1.Input, { className: "searchbar-input", value: this.state.searchText, inverted: true, fluid: true, icon: React.createElement(Icon_1.Icon, { iconName: "Filter", className: "filter-icon" }), iconPosition: "start", placeholder: this.localize("filterByKeywordPlaceholder"), onChange: this.onSearchStateChange })),
                    React.createElement(react_northstar_1.Flex.Item, { push: true },
                        React.createElement("div", null)),
                    React.createElement("div", { className: "filter-bar-item-container" },
                        React.createElement(popup_menu_wrapper_1.default, { title: this.localize("type"), showSearchBar: false, selectedSortBy: this.state.selectedSortBy, checkboxes: this.state.typeList, onRadiogroupStateChange: this.onSortByStateChange, onCheckboxStateChange: this.onTypeCheckboxStateChange }),
                        React.createElement(popup_menu_wrapper_1.default, { title: this.localize("sharedBy"), showSearchBar: true, selectedSortBy: this.state.selectedSortBy, checkboxes: this.state.sharedByList, onRadiogroupStateChange: this.onSortByStateChange, onCheckboxStateChange: this.onSharedByCheckboxStateChange }),
                        React.createElement(popup_menu_wrapper_1.default, { title: this.localize("tagsLabel"), showSearchBar: true, selectedSortBy: this.state.selectedSortBy, checkboxes: this.state.tagsList, onRadiogroupStateChange: this.onSortByStateChange, onCheckboxStateChange: this.onTagsCheckboxStateChange }),
                        React.createElement(popup_menu_wrapper_1.default, { title: this.localize("sortBy"), selectedSortBy: this.state.selectedSortBy, radioGroup: this.state.sortBy, onRadiogroupStateChange: this.onSortByStateChange, onCheckboxStateChange: this.onTagsCheckboxStateChange })),
                    React.createElement("div", null,
                        React.createElement(react_icons_northstar_1.CloseIcon, { className: "close-icon", onClick: this.onCloseIconClick }))),
                this.state.screenWidth <= 750 && React.createElement(react_northstar_1.Flex, { gap: "gap.small", vAlign: "start", className: "filter-bar-wrapper" },
                    React.createElement(react_northstar_1.Flex.Item, { grow: true },
                        React.createElement(react_northstar_1.Flex, { column: true, gap: "gap.small", vAlign: "stretch" },
                            React.createElement("div", { className: "searchbar-wrapper-mobile" },
                                React.createElement(react_northstar_1.Input, { value: this.state.searchText, inverted: true, fluid: true, icon: React.createElement(Icon_1.Icon, { iconName: "Filter", className: "filter-icon" }), iconPosition: "start", placeholder: this.localize("filterByKeywordPlaceholder"), onChange: this.onSearchStateChange })),
                            React.createElement(react_northstar_1.Flex, null,
                                React.createElement(popup_menu_wrapper_1.default, { title: this.localize("type"), showSearchBar: false, selectedSortBy: this.state.selectedSortBy, checkboxes: this.state.typeList, onRadiogroupStateChange: this.onSortByStateChange, onCheckboxStateChange: this.onTypeCheckboxStateChange }),
                                React.createElement(popup_menu_wrapper_1.default, { title: this.localize("sharedBy"), showSearchBar: true, selectedSortBy: this.state.selectedSortBy, checkboxes: this.state.sharedByList, onRadiogroupStateChange: this.onSortByStateChange, onCheckboxStateChange: this.onSharedByCheckboxStateChange }),
                                React.createElement(popup_menu_wrapper_1.default, { title: this.localize("tagsLabel"), showSearchBar: true, selectedSortBy: this.state.selectedSortBy, checkboxes: this.state.tagsList, onRadiogroupStateChange: this.onSortByStateChange, onCheckboxStateChange: this.onTagsCheckboxStateChange }),
                                React.createElement(popup_menu_wrapper_1.default, { title: this.localize("sortBy"), selectedSortBy: this.state.selectedSortBy, radioGroup: this.state.sortBy, onRadiogroupStateChange: this.onSortByStateChange, onCheckboxStateChange: this.onTagsCheckboxStateChange })))),
                    React.createElement(react_northstar_1.Flex.Item, { push: true },
                        React.createElement(react_icons_northstar_1.CloseIcon, { className: "close-icon", onClick: this.onCloseIconClick })))));
        }
        else {
            return (React.createElement(React.Fragment, null));
        }
    };
    return FilterBar;
}(React.Component));
exports.default = react_i18next_1.withTranslation()(FilterBar);
//# sourceMappingURL=filter-bar.js.map