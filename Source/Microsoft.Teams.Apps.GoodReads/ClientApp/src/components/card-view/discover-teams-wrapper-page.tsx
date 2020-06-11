// <copyright file="discover-teams-wrapper-page.tsx" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

import * as React from "react";
import { Loader } from "@fluentui/react-northstar";
import Card from "./card";
import NoPostAddedPage from "./no-post-added-page";
import FilterNoPostContentPage from "./filter-no-post-content-page";
import TitleBar from "../filter-bar-teams/title-bar-teams";
import { Container, Col, Row } from "react-bootstrap";
import * as microsoftTeams from "@microsoft/teams-js";
import { generateColor } from "../../helpers/helper";
import { getTeamDiscoverPosts, getUserVotes, getFilteredPosts, filterTitleAndTagsTeam } from "../../api/discover-api";
import NotificationMessage from "../notification-message/notification-message";
import { WithTranslation, withTranslation } from "react-i18next";
import { TFunction } from "i18next";
import { ICheckBoxItem } from "../filter-bar/filter-bar";
import Resources from "../../constants/resources";
import { getConfigTags } from "../../api/teams-config-tab-api";
import { IDiscoverPost } from "./discover-wrapper-page";
import InfiniteScroll from 'react-infinite-scroller';

import 'bootstrap/dist/css/bootstrap.min.css';
import "../../styles/site.css";

export interface IUserVote {
    postId: string;
    userId: string;
}

interface ICardViewState {
    loader: boolean;
    resourceStrings: any;
    discoverPosts: Array<IDiscoverPost>;
    discoverSearchPosts: Array<IDiscoverPost>;
    alertMessage: string;
    alertType: number;
    showAlert: boolean;
    searchText: string;
    showNoPostPage: boolean;
    tagList: Array<string>;
    infiniteScrollParentKey: number;
    isFilterApplied: boolean;
    isPageInitialLoad: boolean;
    pageLoadStart: number;
    hasMorePosts: boolean;
}

class DiscoverWrapperPage extends React.Component<WithTranslation, ICardViewState> {

    localize: TFunction;
    selectedSharedBy: Array<ICheckBoxItem>;
    selectedPostType: Array<ICheckBoxItem>;
    selectedTags: Array<ICheckBoxItem>;
    selectedSortBy: string;
    filterSearchText: string;
    allPosts: Array<IDiscoverPost>;
    loggedInUserObjectId: string;
    teamId: string;
    authorAvatarBackground: Array<any>;

    constructor(props: any) {
        super(props);
        this.localize = this.props.t;
        this.selectedSharedBy = [];
        this.selectedPostType = [];
        this.selectedTags = [];
        this.selectedSortBy = "";
        this.filterSearchText = "";
        this.allPosts = [];
        this.loggedInUserObjectId = "";
        this.teamId = "";
        this.authorAvatarBackground = [];

        this.state = {
            loader: false,
            discoverPosts: [],
            discoverSearchPosts: [],
            resourceStrings: {},
            alertMessage: "",
            alertType: 0,
            showAlert: false,
            searchText: "",
            showNoPostPage: false,
            tagList: [],
            infiniteScrollParentKey: 0,
            isFilterApplied: false,
            isPageInitialLoad: true,
            pageLoadStart: -1,
            hasMorePosts: true
        }
    }

    /**
    * Used to initialize Microsoft Teams sdk
    */
    async componentDidMount() {
        this.setState({ loader: true });
        microsoftTeams.initialize();
        microsoftTeams.getContext(async (context: microsoftTeams.Context) => {
            this.teamId = context.teamId!;
            this.loggedInUserObjectId = context.userObjectId!;
            this.getConfigTags();
        });
    }

    /**
    * Get tags configured for team
    */
    getConfigTags = async () => {
        let response = await getConfigTags(this.teamId);
        if (response.status === 200 && response.data) {
            this.setState({ tagList: response.data.tags, loader: false });
        }
    }

    /**
    * Get comma separated selected filter entities string.
    * @param filterEntity Array of selected filter entities.
    */
    private getFilterString(filterEntity: Array<string>) {
        return filterEntity.length > 1 ? filterEntity.join(";") : filterEntity.length == 1 ? filterEntity.join(";") + ";" : "";
    }

    /**
    * Get filtered posts based on selected checkboxes.
    * @param pageCount Page count for which next set of posts needs to be fetched
    */
    getFilteredDiscoverPosts = async (pageCount: number) => {
        let postTypes = this.selectedPostType.map((postType: ICheckBoxItem) => { return postType.key.toString() });
        let postTypesString = this.getFilterString(postTypes);
        let authors = this.selectedSharedBy.map((authors: ICheckBoxItem) => { return authors.title });
        let authorsString = this.getFilterString(authors);
        let tags = this.selectedTags.map((tag: ICheckBoxItem) => { return tag.title });
        let tagsString = this.getFilterString(tags);

        let response = await getFilteredPosts(postTypesString, authorsString, tagsString, this.selectedSortBy, this.teamId, pageCount);
        if (response.status === 200 && response.data) {
            if (!response.data.length) {
                this.setState({
                    hasMorePosts: false,
                });
            }

            response.data.map((post: IDiscoverPost) => {
                let searchedAuthor = this.authorAvatarBackground.find((author) => author.id === post.userId);
                if (searchedAuthor) {
                    post.avatarBackgroundColor = searchedAuthor.color;
                }
                else {
                    let color = generateColor();
                    this.authorAvatarBackground.push({ id: post.userId, color: color });
                    post.avatarBackgroundColor = color;
                }

                if (post.userId === this.loggedInUserObjectId) {
                    post.isCurrentUserPost = true;
                }
                else {
                    post.isCurrentUserPost = false;
                }

                this.allPosts.push(post);
            });

            if (response.data.count !== 0) {
                this.setState({
                    isPageInitialLoad: false,
                });
            }
            else {
                this.setState({
                    showNoPostPage: true,
                    isPageInitialLoad: false
                })
            }
            this.getUserVotes();
        }
    }

    /**
    * Fetch posts for Team tab from API
    * @param pageCount Page count for which next set of posts needs to be fetched
    */
    getDiscoverPosts = async (pageCount: number) => {
        let response = await getTeamDiscoverPosts(this.teamId, pageCount);
        if (response.status === 200 && response.data) {
            if (!response.data.length) {
                this.setState({
                    hasMorePosts: false,
                });
            }
            response.data.map((post: IDiscoverPost) => {
                let searchedAuthor = this.authorAvatarBackground.find((author) => author.id === post.userId);
                if (searchedAuthor) {
                    post.avatarBackgroundColor = searchedAuthor.color;
                }
                else {
                    let color = generateColor();
                    this.authorAvatarBackground.push({ id: post.userId, color: color });
                    post.avatarBackgroundColor = color;
                }

                if (post.userId === this.loggedInUserObjectId) {
                    post.isCurrentUserPost = true;
                }
                else {
                    post.isCurrentUserPost = false;
                }

                this.allPosts.push(post);
            });

            if (response.data.count === 0) {
                this.setState({
                    showNoPostPage: true
                })
            }
            this.getUserVotes();
        }

        this.setState({
            loader: false,
            searchText: "",
            isPageInitialLoad: false
        });
    }

    /**
    *Sets state for showing alert notification.
    *@param content Notification message
    *@param type Boolean value indicating 1- Success 2- Error
    */
    showAlert = (content: string, type: number) => {
        this.setState({ alertMessage: content, alertType: type, showAlert: true }, () => {
            setTimeout(() => {
                this.setState({ showAlert: false })
            }, 4000);
        });
    }

    /**
    *Sets state for hiding alert notification.
    */
    hideAlert = () => {
        this.setState({ showAlert: false })
    }

    /**
    * Fetch user votes from API
    */
    getUserVotes = async () => {
        let response = await getUserVotes();
        if (response.status === 200 && response.data) {
            for (let i = 0; i < response.data.length; i++) {
                this.allPosts.map((post: IDiscoverPost) => {
                    if (post.postId === response.data[i].postId) {
                        post.isVotedByUser = true;

                        if (post.totalVotes === 0) {
                            post.totalVotes = 1;
                        }
                    }
                })
            }

            this.onFilterSearchTextChange(this.filterSearchText);
        }

        this.setState({
            loader: false
        });
    }

    /**
    *Removes selected blog post from page
    *@param postId Id of post which needs to be deleted
    *@param isSuccess Boolean indication whether operation succeeded
    */
    handleDeleteButtonClick = (postId: string, isSuccess: boolean) => {
        if (isSuccess) {
            this.allPosts.map((post: IDiscoverPost) => {
                if (post.postId === postId) {
                    post.isRemoved = true;
                }
            });

            this.onFilterSearchTextChange(this.filterSearchText);
        }
    }

    /**
    *Deletes selected blog post
    *@param isSuccess Boolean indication whether operation succeeded
    *@param message Message to be displayed in notification
    */
    handleUserPrivatePostButtonClick = (isSuccess: boolean, message?: string) => {
        if (isSuccess) {
            this.showAlert(this.localize("addPostToPrivateListSuccess"), 1);
        }
        else {
            if (message) {
                this.showAlert(message, 2);
            }
            else {
                this.showAlert(this.localize("addPrivatePostError"), 2);
            }
        }
    }

    /**
    *Invoked by Infinite scroll component when user scrolls down to fetch next set of posts
    *@param pageCount Page count for which next set of posts needs to be fetched
    */
    loadMorePosts = (pageCount: number) => {
        if (!this.filterSearchText.trim().length) {
            if (this.state.searchText.trim().length) {
                this.searchFilterPostUsingAPI(pageCount);
            }
            else if (this.state.isFilterApplied) {
                this.getFilteredDiscoverPosts(pageCount);
            }
            else {
                this.getDiscoverPosts(pageCount);
            }
        }
    }

    /**
    *Set state of search text as per user input change
    *@param searchText Search text entered by user
    */
    handleSearchInputChange = (searchText: string) => {
        this.setState({
            searchText: searchText,
            isFilterApplied: false
        });

        if (searchText.length === 0) {
            this.setState({
                isPageInitialLoad: true,
                pageLoadStart: -1,
                infiniteScrollParentKey: this.state.infiniteScrollParentKey + 1,
                discoverPosts: [],
                hasMorePosts: true
            });
            this.allPosts = [];
        }
    }

    /**
    * Filter cards based on user input after pressing enter key.
    * @param page Page count for which next set of posts needs to be fetched
    */
    searchFilterPostUsingAPI = async (page: number) => {
        this.resetAllFilters();
        if (this.state.searchText.trim().length) {
            let response = await filterTitleAndTagsTeam(this.state.searchText, this.teamId, page);

            if (response.status === 200 && response.data) {
                if (!response.data.length) {
                    this.setState({
                        hasMorePosts: false,
                    });
                }
                response.data.map((post: IDiscoverPost) => {
                    let searchedAuthor = this.authorAvatarBackground.find((author) => author.id === post.userId);
                    if (searchedAuthor) {
                        post.avatarBackgroundColor = searchedAuthor.color;
                    }
                    else {
                        let color = generateColor();
                        this.authorAvatarBackground.push({ id: post.userId, color: color });
                        post.avatarBackgroundColor = color;
                    }

                    if (post.userId === this.loggedInUserObjectId) {
                        post.isCurrentUserPost = true;
                    }
                    else {
                        post.isCurrentUserPost = false;
                    }

                    this.allPosts.push(post)
                });

                this.setState({ isPageInitialLoad: false });
                this.getUserVotes();
            }
        }
    }

    /**
    *Filter cards based on 'shared by' checkbox selection.
    *@param selectedCheckboxes User selected checkbox array
    */
    onSharedByCheckboxStateChange = (selectedCheckboxes: Array<ICheckBoxItem>) => {
        this.selectedSharedBy = selectedCheckboxes.filter((value) => { return value.isChecked });
        this.setState({
            isPageInitialLoad: true,
            pageLoadStart: -1,
            infiniteScrollParentKey: this.state.infiniteScrollParentKey + 1,
            discoverPosts: [],
            searchText: "",
            hasMorePosts: true
        });

        this.allPosts = [];
    }

    /**
    *Filter cards based on post type checkbox selection.
    *@param selectedCheckboxes User selected checkbox array
    */
    onTypeCheckboxStateChange = (selectedCheckboxes: Array<ICheckBoxItem>) => {
        this.selectedPostType = selectedCheckboxes.filter((value) => { return value.isChecked });
        this.setState({
            isPageInitialLoad: true,
            pageLoadStart: -1,
            infiniteScrollParentKey: this.state.infiniteScrollParentKey + 1,
            discoverPosts: [],
            searchText: "",
            hasMorePosts: true
        });

        this.allPosts = [];
    }

    /**
    *Filter cards based on tags checkbox selection.
    *@param selectedCheckboxes User selected checkbox array
    */
    onTagsStateChange = (selectedCheckboxes: Array<ICheckBoxItem>) => {
        this.selectedTags = selectedCheckboxes.filter((value) => { return value.isChecked });
        this.setState({
            isPageInitialLoad: true,
            pageLoadStart: -1,
            infiniteScrollParentKey: this.state.infiniteScrollParentKey + 1,
            discoverPosts: [],
            searchText: "",
            hasMorePosts: true
        });

        this.allPosts = [];
    }

    /**
    *Filter cards based sort by value.
    *@param selectedValue Selected value for 'sort by'
    */
    onSortByChange = (selectedValue: string) => {
        this.selectedSortBy = selectedValue;
        this.setState({
            isPageInitialLoad: true,
            pageLoadStart: -1,
            infiniteScrollParentKey: this.state.infiniteScrollParentKey + 1,
            discoverPosts: [],
            searchText: "",
            hasMorePosts: true
        });

        this.allPosts = [];
    }

    /**
    * Invoked when user clicks on vote icon. Updates state and showing notification alert.
    * @param isSuccess Boolean indicating whether edit operation is successful.
    * @param isLiked Boolean indicating whether post is liked or not.
    */
    onVoteClick = (isSuccess: boolean, isLiked: boolean) => {
        if (isSuccess) {
            if (isLiked) {
                this.showAlert(this.localize("voteSuccess"), 1)
            }
            else {
                this.showAlert(this.localize("voteUnliked"), 1)
            }
        }
        else {
            this.showAlert(this.localize("voteError"), 2)
        }
    }

    /**
    * Reset app user selected filters
    */
    resetAllFilters = () => {
        this.selectedSortBy = Resources.sortBy[0].id;
        this.selectedSharedBy = [];
        this.selectedPostType = [];
        this.selectedTags = [];
        this.filterSearchText = "";
    }

    /**
    * Invoked when post is edited. Updates state and shows notification alert.
    * @param cardDetails Updated post details
    * @param isSuccess Boolean indicating whether edit operation is successful.
    */
    onCardUpdate = (cardDetails: IDiscoverPost, isSuccess: boolean) => {
        if (isSuccess) {
            this.allPosts.map((post: IDiscoverPost) => {
                if (post.postId === cardDetails.postId) {
                    post.description = cardDetails.description;
                    post.title = cardDetails.title;
                    post.contentUrl = cardDetails.contentUrl;
                    post.tags = cardDetails.tags;
                }
            });

            this.setState({ discoverPosts: this.allPosts });
            this.onFilterSearchTextChange(this.filterSearchText);
            this.showAlert(this.localize("postUpdateSuccess"), 1)
        }
        else {
            this.showAlert(this.localize("postUpdateError"), 2)
        }
    }

    /**
    * Invoked when new post is added. Shows notification alert.
    * @param isSuccess Boolean indicating whether add new post operation is successful.
    * @param getSubmittedPost Post details which needs to be added.
    */
    onNewPost = (isSuccess: boolean, getSubmittedPost: IDiscoverPost) => {
        if (isSuccess) {
            let searchedAuthor = this.authorAvatarBackground.find((author) => author.id === getSubmittedPost.userId);
            if (searchedAuthor) {
                getSubmittedPost.avatarBackgroundColor = searchedAuthor.color;
            }
            else {
                let color = generateColor();
                this.authorAvatarBackground.push({ id: getSubmittedPost.userId, color: color });
                getSubmittedPost.avatarBackgroundColor = color;
            }

            let submittedPost = this.state.discoverPosts;
            if (getSubmittedPost.userId === this.loggedInUserObjectId) {
                getSubmittedPost.isCurrentUserPost = true;
            }
            else {
                getSubmittedPost.isCurrentUserPost = false;
            }
            submittedPost.unshift(getSubmittedPost);
            this.setState({ discoverPosts: submittedPost });
            this.allPosts = this.state.discoverPosts;
            this.showAlert(this.localize("addNewPostSuccess"), 1)
        }
        else {
            this.showAlert(this.localize("addNewPostError"), 2)
        }
    }

    /**
    * Invoked when either filter bar is displayed or closed
    * @param isOpen Boolean indicating whether filter bar is displayed or closed.
    */
    handleFilterClear = (isOpen: boolean) => {
        this.resetAllFilters();
        this.setState({
            isPageInitialLoad: true,
            pageLoadStart: -1,
            infiniteScrollParentKey: this.state.infiniteScrollParentKey + 1,
            discoverPosts: [],
            searchText: "",
            isFilterApplied: isOpen,
            hasMorePosts: true
        });
        this.allPosts = [];
    }

    /**
    * Invoked when user hits enter or clicks on search icon for searching post through command bar
    */
    invokeApiSearch = () => {
        this.setState({
            isPageInitialLoad: true,
            pageLoadStart: -1,
            infiniteScrollParentKey: this.state.infiniteScrollParentKey + 1,
            discoverPosts: [],
            hasMorePosts: true
        });
        this.allPosts = [];
    }

    /**
    * Search inline with retrieved post from API as per user input.
    * @param searchText Search text entered by user.
    */
    onFilterSearchTextChange = (searchText: string) => {
        this.filterSearchText = searchText;
        if (searchText.trim().length) {
            let filteredPosts = this.allPosts.filter((post: IDiscoverPost) => {
                return post.title.toLowerCase().includes(searchText.toLowerCase()) === true;
            });

            this.setState({ discoverPosts: filteredPosts });
        }
        else {
            this.setState({ discoverPosts: [...this.allPosts] });
        }
    }

    /**
    * Renders the component
    */
    public render(): JSX.Element {
        return (
            <div>
                {this.getWrapperPage()}
            </div>
        );
    }

    /**
    *Get wrapper for page which acts as container for all child components
    */
    private getWrapperPage = () => {
        if (this.state.loader) {
            return (
                <div className="container-div">
                    <div className="container-subdiv">
                        <div className="loader">
                            <Loader />
                        </div>
                    </div>
                </div>
            );
        } else {
            // Cards component array to be rendered in grid.
            const cards = new Array<any>();

            this.state.discoverPosts!.map((value: IDiscoverPost, index) => {
                if (!value.isRemoved) {
                    cards.push(<Col lg={3} sm={6} md={4} className="grid-column d-flex justify-content-center">
                        <Card onAddPrivatePostClick={this.handleUserPrivatePostButtonClick} index={index} cardDetails={value} onVoteClick={this.onVoteClick} onCardUpdate={this.onCardUpdate} onDeleteButtonClick={this.handleDeleteButtonClick} />
                    </Col>);
                }
            });

            if (this.state.tagList.length > 0 && this.state.discoverPosts.length >= 0) {
                let scrollViewStyle = { height: this.state.isFilterApplied === true ? "84vh" : "92vh" };
                return (
                    <div className="container-div">
                        <div className="container-subdiv-cardview">
                            <Container className="container-fluid-overriden" fluid>
                                <NotificationMessage
                                    onClose={this.hideAlert}
                                    showAlert={this.state.showAlert}
                                    content={this.state.alertMessage}
                                    notificationType={this.state.alertType} />
                                <TitleBar
                                    commandBarSearchText={this.state.searchText}
                                    searchFilterPostsUsingAPI={this.invokeApiSearch}
                                    onFilterClear={this.handleFilterClear}
                                    hideFilterbar={!this.state.isFilterApplied}
                                    onSortByChange={this.onSortByChange}
                                    onFilterSearchChange={this.onFilterSearchTextChange}
                                    onSearchInputChange={this.handleSearchInputChange}
                                    onNewPostSubmit={this.onNewPost}
                                    onSharedByCheckboxStateChange={this.onSharedByCheckboxStateChange}
                                    onTypeCheckboxStateChange={this.onTypeCheckboxStateChange}
                                    onTagsStateChange={this.onTagsStateChange} />
                                <div key={this.state.infiniteScrollParentKey} className="scroll-view" style={scrollViewStyle}>
                                    <InfiniteScroll
                                        pageStart={this.state.pageLoadStart}
                                        loadMore={this.loadMorePosts}
                                        hasMore={this.state.hasMorePosts && !this.filterSearchText.trim().length}
                                        initialLoad={this.state.isPageInitialLoad}
                                        useWindow={false}
                                        loader={<div className="loader"><Loader /></div>}>

                                        <Row>
                                            {
                                                cards.length > 0 ? cards : this.state.hasMorePosts === true ? <></> : <FilterNoPostContentPage />
                                            }
                                        </Row>

                                    </InfiniteScroll>
                                </div>
                            </Container>
                        </div>
                    </div>
                );
            }
            else {
                return (
                    <div className="container-div">
                        <div className="container-subdiv">
                            <NotificationMessage onClose={this.hideAlert} showAlert={this.state.showAlert} content={this.state.alertMessage} notificationType={this.state.alertType} />
                            <NoPostAddedPage onNewPostSubmit={this.onNewPost} />
                        </div>
                    </div>
                )
            }
        }
    }
}
export default withTranslation()(DiscoverWrapperPage)