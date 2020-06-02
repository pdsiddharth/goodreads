// <copyright file="popup-more-menu.tsx" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

import * as React from "react";
import { Popup, Flex, Text, Divider } from "@fluentui/react-northstar";
import { AddIcon, TrashCanIcon, MoreIcon } from "@fluentui/react-icons-northstar";
import EditItemDialog from "../edit-dialog/edit-dialog";
import { Container } from "react-bootstrap";
import { IDiscoverPost } from "./discover-wrapper-page";
import { useTranslation } from 'react-i18next';

import "../../styles/card.css";

interface IPopupMoreMenu {
	cardDetails: IDiscoverPost;
	onMenuItemClick: (key: any) => void;
	onEditSubmit: (editedCardDetails: any, isSuccess: boolean) => void;
}

const PopupMoreMenu: React.FunctionComponent<IPopupMoreMenu> = props => {
	const localize = useTranslation().t;
	const [menuOpen, setMenuOpen] = React.useState(false);

	/**
    *Invoked while closing dialog. Set state to original values.
    */
	const onCancel = () => {
		setMenuOpen(false);
	}

	/**
	*Invoked when edit post detail is successful from dialog.
	*@param cardDetails Updated post details
	*@param isSuccess Boolean indication whether operation result
    */
	const onEditSubmit = (cardDetails: IDiscoverPost, isSuccess: boolean) => {
		setMenuOpen(false);
		props.onEditSubmit(cardDetails, isSuccess);
	}

	/**
	*Invoked when menu item is clicked and passes back to parent component.
	*@param key Selected menu item key
    */
	const onItemClick = (key: number) => {
		if (key === 1 || key === 3) {
			setMenuOpen(false);
		}
		props.onMenuItemClick(key);
	}

	return (
		<Popup
			onOpenChange={(e, { open }: any) => setMenuOpen(open)}
			open={menuOpen}
			content={
				<Container fluid className="popup-menu-content-wrapper">
					<Flex vAlign="center" className="menu-items-wrapper" onClick={(event: any) => onItemClick(1)}>
						<AddIcon outline /> <Text className="popup-menu-item-text" content={localize("addToPrivateList")} />
					</Flex>
					{props.cardDetails.isCurrentUserPost && <><EditItemDialog
						index={1}
						cardDetails={props.cardDetails}
						onSubmit={onEditSubmit}
						onCancel={onCancel}
					/>
						<Divider />
						<Flex vAlign="center" className="menu-items-wrapper" onClick={(event: any) => onItemClick(3)}>
							<TrashCanIcon outline /> <Text className="popup-menu-item-text" content={localize("delete")} />
						</Flex></>}
				</Container>
			}
			trigger={<MoreIcon className="more-menu-icon" />}
		/>
	);
}

export default React.memo(PopupMoreMenu);