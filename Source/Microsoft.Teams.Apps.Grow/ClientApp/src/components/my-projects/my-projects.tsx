// <copyright file="title-bar.tsx" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

import * as React from "react";
import { Loader, Menu } from "@fluentui/react-northstar";
import { getAuthors, getTags } from "../../api/discover-api";
import MyCreatedProjects from "../my-projects/my-created-projects";
import MyJoinedProjects from "../my-joined-projects/my-joined-projects";
import { IProjectDetails } from "../card-view/discover-wrapper-page";

import "../../styles/projects-cards.css";
import "../../styles/join-project-dialog.css";

interface IFilterBarProps {

}

interface IFilterBarState {
    activeIndex: number;
    joinedCount: number;
    createdCount: number;
}

class TitleBar extends React.Component<IFilterBarProps, IFilterBarState> {
    constructor(props: IFilterBarProps) {
        super(props);

        this.state = {
            activeIndex: 0,
            joinedCount: 0,
            createdCount: 0
        }
    }

    onMenuItemClick = (e: any, props: any) => {
        this.setState({
            activeIndex: props.activeIndex
        })
    }

    showCreatedProjectCount = (count : number) => {
        this.setState({
            createdCount: count
        })
    }

    showJoinedProjectCount = (count: number) => {
        this.setState({
            joinedCount: count
        })
    }

	/**
	* Renders the component
	*/
    public render(): JSX.Element {

        let joinedCount = "";
        let createdCount = "";

        if (this.state.joinedCount !== 0 && this.state.activeIndex === 1) {
            joinedCount = '(' + this.state.joinedCount + ')'
        }
        else {
            joinedCount = "";
        }

        if (this.state.createdCount !== 0 && this.state.activeIndex === 0) {
            createdCount = '(' + this.state.createdCount + ')'
        }
        else {
            createdCount = "";
        }

        const items = [
            {
                key: 'Created project',
                content: 'Created project ' + createdCount + '',

            },
            {
                key: 'Joined Projects',
                content: 'Joined Projects ' + joinedCount + '',
            }
        ]
        return (
            <>
                <div className="container-div">
                    <div className="container-subdiv-myprojects">
                        <Menu
                            className="project-menu"
                            defaultActiveIndex={0}
                            items={items}
                            onActiveIndexChange={(e: any, props: any) => this.onMenuItemClick(e, props)}
                            primary />
                    
                {
                    this.state.activeIndex === 0
                                ? <MyCreatedProjects showProjectCount={this.showCreatedProjectCount} />
                                : <MyJoinedProjects showProjectCount={this.showJoinedProjectCount} />
                        }
                    </div>
                </div>
            </>
        )
    }
}

export default TitleBar;