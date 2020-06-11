// <copyright file="more-menu-content.tsx" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

import * as React from "react";
import { Flex, Text, Divider } from "@fluentui/react-northstar";
import { AddIcon, TrashCanIcon } from "@fluentui/react-icons-northstar";
import EditItemDialog from "../edit-dialog/edit-dialog";
import { Container } from "react-bootstrap";
import { IDiscoverPost } from "./discover-wrapper-page";
import { useTranslation } from 'react-i18next';

import "../../styles/more-menu-content.css";

interface IMoreMenuContentProps {
    cardDetails: IDiscoverPost;
    onMenuItemClick: (key: any) => void;
    onCancel: () => void;
    onEditSubmit: (editedCardDetails: any, isSuccess: boolean) => void;
}

const MoreMenuContent: React.FunctionComponent<IMoreMenuContentProps> = props => {
    const localize = useTranslation().t;
    return (
        <Container fluid className="popup-menu-content-wrapper">
            <Flex vAlign="center" className="menu-items-wrapper" onClick={(event: any) => props.onMenuItemClick(1)}>
                <AddIcon outline /> <Text className="popup-menu-item-text" content={localize("addToPrivateList")} />
            </Flex>
            {props.cardDetails.isCurrentUserPost && <><EditItemDialog
                index={1}
                cardDetails={props.cardDetails}
                onSubmit={props.onEditSubmit}
                onCancel={props.onCancel}
            />
                <Divider />
                <Flex vAlign="center" className="menu-items-wrapper" onClick={(event: any) => props.onMenuItemClick(3)}>
                    <TrashCanIcon outline /> <Text className="popup-menu-item-text" content={localize("delete")} />
                </Flex></>}
        </Container>
    );
}

export default MoreMenuContent;